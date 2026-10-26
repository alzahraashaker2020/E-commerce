using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Shop.DAL.Data;
using Shop.DAL.Models;

internal static class PostgresChecks
{
    public static void Run()
    {
        var connectionString = Environment.GetEnvironmentVariable("SHOP_TEST_POSTGRES")
            ?? throw new InvalidOperationException("SHOP_TEST_POSTGRES is required for the isolated test cluster.");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Host != "127.0.0.1" || builder.Port == 5432)
            throw new InvalidOperationException("Tests require an isolated loopback cluster on a nondefault port.");
        using var admin = new NpgsqlConnection(connectionString); admin.Open();
        int checks = 0;
        void Expect(bool value, string label) { if (!value) throw new Exception(label); checks++; }
        foreach (var scenario in new[] { "fresh", "script", "legacy", "rollback" })
        {
            string name = "shop_check_" + scenario + "_" + Guid.NewGuid().ToString("N");
            using (var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin)) create.ExecuteNonQuery();
            builder.Database = name;
            using var db = new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseNpgsql(builder.ConnectionString).Options);
            object? Scalar(string sql)
            {
                db.Database.OpenConnection();
                using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql;
                return command.ExecuteScalar();
            }
            long Count(string sql) => Convert.ToInt64(Scalar(sql));
            var migrator = db.GetService<IMigrator>();
            if (scenario is "legacy" or "rollback")
            {
                migrator.Migrate("20261022182410_InitialCreate");
                db.Database.ExecuteSqlRaw("UPDATE \"Products\" SET \"Name\"='Merchant custom name' WHERE \"Id\"=(SELECT MIN(\"Id\") FROM \"Products\")");
                if (scenario == "rollback") db.Database.ExecuteSqlRaw("UPDATE \"InventoryItems\" SET \"QuantityOnHand\"=-1");
            }
            if (scenario == "rollback")
            {
                bool rejected = false;
                try { db.Database.Migrate(); } catch (PostgresException) { rejected = true; }
                Expect(rejected, "Negative legacy inventory must reject upgrade");
                Expect(Count("SELECT COUNT(*) FROM \"__EFMigrationsHistory\"") == 1, "Failed upgrade keeps initial history");
                Expect(Count("SELECT COUNT(*) FROM information_schema.columns WHERE table_name='Products' AND column_name='StockQuantity'") == 0, "Failed upgrade rolls back new columns");
                Console.WriteLine("PASS: PostgreSQL transactional rollback");
                continue;
            }
            long beforeProducts = scenario == "legacy" ? Count("SELECT COUNT(*) FROM \"Products\"") : 0;
            long beforeVariants = scenario == "legacy" ? Count("SELECT COUNT(*) FROM \"ProductVariants\"") : 0;
            if (scenario == "script")
                db.Database.ExecuteSqlRaw(migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent));
            db.Database.Migrate();
            Expect(!db.Database.GetPendingMigrations().Any(), "All migrations applied");
            Expect(db.Products.AsNoTracking().Any(), "Current product model reads migrated catalog");
            _ = db.ProductVariants.AsNoTracking().ToList();
            _ = db.Orders.AsNoTracking().ToList();
            _ = db.OrderItems.AsNoTracking().ToList();
            _ = db.Currencies.AsNoTracking().ToList();
            _ = db.OrderStatusDefinitions.AsNoTracking().ToList();
            _ = db.StoreSettings.AsNoTracking().ToList();
            _ = db.Governorates.AsNoTracking().ToList();
            _ = db.DeliveryAreas.AsNoTracking().ToList();
            Expect(Count("SELECT COUNT(*) FROM \"ProductVariants\" WHERE \"Id\"=1012") == 1, "Original variant 1012 preserved");
            Expect(Count("SELECT COUNT(*) FROM \"LegacyInventoryItems\"") > 0, "Original inventory retained");
            Expect(Count("SELECT COUNT(*) FROM \"ProductVariants\" v WHERE v.\"StockQuantity\" <> COALESCE((SELECT SUM(i.\"QuantityOnHand\") FROM \"LegacyInventoryItems\" i WHERE i.\"VariantId\"=v.\"Id\"),0)") == 0, "Variant stock preserved exactly");
            if (scenario == "legacy")
            {
                Expect(db.Products.Count() == beforeProducts, "Merchant products preserved");
                Expect(db.ProductVariants.Count() == beforeVariants, "Merchant variants preserved");
                Expect(db.Products.Any(p => p.Name == "Merchant custom name"), "Merchant edits preserved");
            }
            var variant = new ProductVariant { ProductId = db.Products.OrderBy(p => p.Id).First().Id,
                Price = 12.50m, StockQuantity = 37, IsActive = true,
                ImageUrls = new() { "/uploads/check-a.jpg", "/uploads/check-b.jpg" } };
            db.ProductVariants.Add(variant);
            db.SaveChanges();
            db.ChangeTracker.Clear();
            var saved = db.ProductVariants.Single(v => v.Id == variant.Id);
            Expect(saved.StockQuantity == 37 && saved.ImageUrls.Count == 2, "Current variant stock and galleries can be saved");
            var status = new OrderStatusDefinition { Name = "Merchant custom status", Color = "#123456", Behavior = OrderStatus.Pending, IsActive = true };
            db.OrderStatusDefinitions.Add(status); db.SaveChanges();
            Expect(status.Id > 1, "Status identity advances past seeded definitions");
            int productsBeforeScript = db.Products.Count();
            string script = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            db.Database.ExecuteSqlRaw(script);
            db.Database.ExecuteSqlRaw(script);
            Expect(db.Products.Count() == productsBeforeScript, "Idempotent script can run twice without losing catalog");
            Expect(Count("SELECT COUNT(*) FROM \"__EFMigrationsHistory\"") == 11, "Each migration recorded once");
            Console.WriteLine("PASS: PostgreSQL " + scenario + " migration and idempotent SQL");
        }
        Console.WriteLine($"PostgreSQL checks passed: {checks}");
    }
}
