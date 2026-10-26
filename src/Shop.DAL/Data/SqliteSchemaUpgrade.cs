using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.DAL.Models;

namespace Shop.DAL.Data;

/// <summary>Versioned, transactional upgrades for the SQLite development database.</summary>
public static class SqliteSchemaUpgrade
{
    /// <summary>Latest SQLite schema version. PostgreSQL uses its separate migration chain.</summary>
    public const int LatestVersion = 9;

    /// <summary>Validates the baseline, backs up existing files, and upgrades once.</summary>
    public static void Apply(ShopContext db, bool newlyCreated = false)
    {
        db.Database.OpenConnection();
        try
        {
            int version = ReadVersion(db);
            if (version > LatestVersion)
                throw new InvalidOperationException("This database is newer than the application. Use a compatible application version.");
            RequireColumns(db, "Products", "HasVariants", "ShowStockQuantity", "StockQuantity", "LowStockThreshold");
            RequireColumns(db, "ProductVariants", "SizeId", "ColorId", "StockQuantity", "LowStockThreshold", "IsActive", "ImageUrl");
            string? backupPath = !newlyCreated && version < LatestVersion ? Backup(db) : null;
            using var transaction = db.Database.BeginTransaction();
            // A second startup may have upgraded while this process waited for the lock.
            version = ReadVersion(db);
            if (version > LatestVersion) throw new InvalidOperationException("Database schema is newer than this application.");
            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"__ShopSchemaVersions\" (\"Version\" INTEGER PRIMARY KEY, \"AppliedAtUtc\" TEXT NOT NULL)");
            for (int next = version + 1; next <= LatestVersion; next++)
            {
                switch (next)
                {
                    case 1: UpgradeStockAndGallery(db); break;
                    case 2: UpgradeConfiguration(db); break;
                    case 3: UpgradeDelivery(db); break;
                    case 4:
                        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_Orders_StatusDefinitionId\" ON \"Orders\" (\"StatusDefinitionId\")");
                        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_Orders_DeliveryAreaId\" ON \"Orders\" (\"DeliveryAreaId\")");
                        break;
                    case 5:
                        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"CheckoutReceipts\" (\"Key\" TEXT PRIMARY KEY, \"Owner\" TEXT NOT NULL, \"RequestHash\" TEXT NOT NULL, \"ResponseJson\" TEXT NOT NULL)");
                        break;
                    case 6:
                        db.Database.ExecuteSqlRaw(AccountSecuritySchema.Sqlite);
                        break;
                    case 7:
                        db.Database.ExecuteSqlRaw(EmailOutboxSchema.Sql);
                        break;
                    case 8:
                        db.Database.ExecuteSqlRaw(ReturnsSchema.Sql);
                        break;
                    case 9:
                        db.Database.ExecuteSqlRaw(StorefrontSchema.Sql);
                        break;
                }
                db.Database.ExecuteSqlRaw("INSERT INTO \"__ShopSchemaVersions\" (\"Version\", \"AppliedAtUtc\") VALUES ({0}, {1})", next, DateTime.UtcNow.ToString("O"));
            }
            ValidateModel(db);
            RequireColumns(db, "CheckoutReceipts", "Key", "Owner", "RequestHash", "ResponseJson");
            transaction.Commit();
            if (backupPath is not null)
                Console.WriteLine("Database upgrade completed. A pre-upgrade backup was retained in the database backups directory.");
        }
        finally { db.Database.CloseConnection(); }
    }

    /// <summary>Reads the recorded schema version without changing the database.</summary>
    public static int ReadVersion(ShopContext db)
    {
        using var exists = Command(db, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__ShopSchemaVersions'");
        if (Convert.ToInt32(exists.ExecuteScalar()) == 0) return 0;
        using var command = Command(db, "SELECT Version FROM __ShopSchemaVersions ORDER BY Version");
        using var reader = command.ExecuteReader();
        int version = 0;
        while (reader.Read())
        {
            int next = reader.GetInt32(0);
            if (next != version + 1) throw new InvalidOperationException("Database schema history has a gap. Restore or repair the schema history before startup.");
            version = next;
        }
        return version;
    }

    private static DbCommand Command(ShopContext db, string sql)
    {
        var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        return command;
    }

    private static HashSet<string> Columns(ShopContext db, string table)
    {
        using var command = Command(db, "PRAGMA table_info(\"" + table.Replace("\"", "\"\"") + "\")");
        using var reader = command.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read()) columns.Add(reader.GetString(1));
        return columns;
    }

    private static void RequireColumns(ShopContext db, string table, params string[] names)
    {
        var actual = Columns(db, table);
        var missing = names.Where(n => !actual.Contains(n)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Unsupported database schema: {table} is missing {string.Join(", ", missing)}. A reviewed data migration is required; the database has not been reset.");
    }

    private static void AddColumn(ShopContext db, string table, string name, string definition)
    {
        if (!Columns(db, table).Contains(name))
            db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{name}\" {definition}");
    }

    private static string? Backup(ShopContext db)
    {
        var source = (SqliteConnection)db.Database.GetDbConnection();
        if (string.IsNullOrWhiteSpace(source.DataSource) || source.DataSource == ":memory:") return null;
        string file = Path.GetFullPath(source.DataSource);
        string directory = Path.Combine(Path.GetDirectoryName(file)!, "backups");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, Path.GetFileName(file) + ".before-v" + LatestVersion + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N") + ".db");
        using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate }.ToString());
        destination.Open();
        source.BackupDatabase(destination);
        return path;
    }

    private static void UpgradeStockAndGallery(ShopContext db)
    {
        AddColumn(db, "Orders", "StockDeducted", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(db, "ProductVariants", "ImageUrls", "TEXT NOT NULL DEFAULT '[]'");
    }

    private static void UpgradeConfiguration(ShopContext db)
    {
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"Currencies\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Code\" TEXT NOT NULL UNIQUE, \"Name\" TEXT NOT NULL, \"Symbol\" TEXT NOT NULL, \"IsActive\" INTEGER NOT NULL)");
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"OrderStatusDefinitions\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL UNIQUE, \"Color\" TEXT NOT NULL, \"Behavior\" INTEGER NOT NULL, \"DisplayOrder\" INTEGER NOT NULL, \"IsActive\" INTEGER NOT NULL, \"IsDefault\" INTEGER NOT NULL)");
        using (var command = Command(db, "SELECT COUNT(*) FROM Currencies"))
        {
            if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                db.Database.ExecuteSqlRaw("INSERT INTO Currencies (Code,Name,Symbol,IsActive) VALUES ('EGP','Egyptian Pound','E£',1),('USD','US Dollar','$',1)");
        }
        using (var command = Command(db, "SELECT COUNT(*) FROM OrderStatusDefinitions"))
        {
            if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                foreach (OrderStatus behavior in Enum.GetValues<OrderStatus>())
                    db.Database.ExecuteSqlRaw("INSERT INTO OrderStatusDefinitions (Id,Name,Color,Behavior,DisplayOrder,IsActive,IsDefault) VALUES ({0},{1},'#8a776e',{2},{2},1,{3})",
                        (int)behavior + 1, behavior.ToString(), (int)behavior, behavior == OrderStatus.Pending);
        }
        AddColumn(db, "Orders", "StatusDefinitionId", "INTEGER NULL REFERENCES \"OrderStatusDefinitions\" (\"Id\")");
        db.Database.ExecuteSqlRaw("UPDATE \"Orders\" SET \"StatusDefinitionId\" = \"Status\" + 1 WHERE \"StatusDefinitionId\" IS NULL AND EXISTS (SELECT 1 FROM \"OrderStatusDefinitions\" WHERE \"Id\" = \"Orders\".\"Status\" + 1)");
        db.Database.ExecuteSqlRaw("INSERT OR IGNORE INTO Currencies (Code,Name,Symbol,IsActive) SELECT CurrencyCode,CurrencyCode,CurrencySymbol,1 FROM StoreSettings");
    }

    private static void UpgradeDelivery(ShopContext db)
    {
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"Governorates\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL UNIQUE, \"IsActive\" INTEGER NOT NULL)");
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"DeliveryAreas\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"GovernorateId\" INTEGER NOT NULL REFERENCES \"Governorates\" (\"Id\"), \"Name\" TEXT NOT NULL, \"ShippingFee\" TEXT NOT NULL, \"CurrencyCode\" TEXT NOT NULL, \"IsActive\" INTEGER NOT NULL, UNIQUE (\"GovernorateId\", \"Name\"))");
        AddColumn(db, "Orders", "DeliveryAreaId", "INTEGER NULL REFERENCES \"DeliveryAreas\" (\"Id\")");
    }

    private static void ValidateModel(ShopContext db)
    {
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null) continue;
            var identifier = StoreObjectIdentifier.Table(table, entity.GetSchema());
            RequireColumns(db, table, entity.GetProperties().Select(p => p.GetColumnName(identifier)!).ToArray());
        }
        using var command = Command(db, "PRAGMA foreign_key_check");
        using var reader = command.ExecuteReader();
        if (reader.Read()) throw new InvalidOperationException("Database contains invalid references. Startup was stopped without deleting data.");
    }
}
