using Microsoft.EntityFrameworkCore;

namespace Shop.DAL.Data;

/// <summary>Additive compatibility updates for existing SQLite demo databases.</summary>
public static class SqliteSchemaUpgrade
{
    /// <summary>Adds the stock marker without deleting existing orders or products.</summary>
    public static void Apply(ShopContext db)
    {
        db.Database.OpenConnection();
        try
        {
            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"Governorates\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL UNIQUE, \"IsActive\" INTEGER NOT NULL)");
            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"DeliveryAreas\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"GovernorateId\" INTEGER NOT NULL REFERENCES \"Governorates\" (\"Id\"), \"Name\" TEXT NOT NULL, \"ShippingFee\" TEXT NOT NULL, \"CurrencyCode\" TEXT NOT NULL, \"IsActive\" INTEGER NOT NULL, UNIQUE (\"GovernorateId\", \"Name\"))");
            using (var schema = db.Database.GetDbConnection().CreateCommand())
            {
                schema.CommandText = "PRAGMA table_info('Orders')";
                bool areaExists = false;
                using (var reader = schema.ExecuteReader())
                    while (reader.Read()) areaExists |= reader.GetString(1) == "DeliveryAreaId";
                if (!areaExists) db.Database.ExecuteSqlRaw("ALTER TABLE \"Orders\" ADD COLUMN \"DeliveryAreaId\" INTEGER NULL REFERENCES \"DeliveryAreas\" (\"Id\")");
            }
            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"Currencies\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Code\" TEXT NOT NULL UNIQUE, \"Name\" TEXT NOT NULL, \"Symbol\" TEXT NOT NULL, \"IsActive\" INTEGER NOT NULL)");
            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"OrderStatusDefinitions\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL UNIQUE, \"Color\" TEXT NOT NULL, \"Behavior\" INTEGER NOT NULL, \"DisplayOrder\" INTEGER NOT NULL, \"IsActive\" INTEGER NOT NULL, \"IsDefault\" INTEGER NOT NULL)");
            // Seed only when upgrading a database that has no configuration yet.
            if (!db.Currencies.Any())
            {
                db.Currencies.AddRange(new Models.Currency { Code = "EGP", Name = "Egyptian Pound", Symbol = "E£" },
                    new Models.Currency { Code = "USD", Name = "US Dollar", Symbol = "$" });
                db.SaveChanges();
            }
            if (!db.OrderStatusDefinitions.Any())
            {
                foreach (Models.OrderStatus behavior in Enum.GetValues<Models.OrderStatus>())
                    db.OrderStatusDefinitions.Add(new Models.OrderStatusDefinition { Id = (int)behavior + 1, Name = behavior.ToString(),
                        Behavior = behavior, DisplayOrder = (int)behavior, IsDefault = behavior == Models.OrderStatus.Pending });
                db.SaveChanges();
            }
            using (var schema = db.Database.GetDbConnection().CreateCommand())
            {
                schema.CommandText = "PRAGMA table_info('Orders')";
                bool statusExists = false;
                using (var reader = schema.ExecuteReader())
                    while (reader.Read()) statusExists |= reader.GetString(1) == "StatusDefinitionId";
                if (!statusExists)
                    db.Database.ExecuteSqlRaw("ALTER TABLE \"Orders\" ADD COLUMN \"StatusDefinitionId\" INTEGER NULL REFERENCES \"OrderStatusDefinitions\" (\"Id\")");
            }
            db.Database.ExecuteSqlRaw("UPDATE \"Orders\" SET \"StatusDefinitionId\" = \"Status\" + 1 WHERE \"StatusDefinitionId\" IS NULL AND EXISTS (SELECT 1 FROM \"OrderStatusDefinitions\" WHERE \"Id\" = \"Orders\".\"Status\" + 1)");
            var currentCurrency = db.StoreSettings.FirstOrDefault();
            if (currentCurrency is not null && !db.Currencies.Any(c => c.Code == currentCurrency.CurrencyCode))
            {
                db.Currencies.Add(new Models.Currency { Code = currentCurrency.CurrencyCode, Name = currentCurrency.CurrencyCode, Symbol = currentCurrency.CurrencySymbol });
                db.SaveChanges();
            }
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA table_info('Orders')";
            bool exists = false;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                    exists |= string.Equals(reader.GetString(1), "StockDeducted", StringComparison.OrdinalIgnoreCase);
            }
            if (!exists)
                db.Database.ExecuteSqlRaw("ALTER TABLE \"Orders\" ADD COLUMN \"StockDeducted\" INTEGER NOT NULL DEFAULT 0");
            command.CommandText = "PRAGMA table_info('ProductVariants')";
            bool galleryExists = false;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                    galleryExists |= string.Equals(reader.GetString(1), "ImageUrls", StringComparison.OrdinalIgnoreCase);
            }
            if (!galleryExists)
                db.Database.ExecuteSqlRaw("ALTER TABLE \"ProductVariants\" ADD COLUMN \"ImageUrls\" TEXT NOT NULL DEFAULT '[]'");
        }
        finally { db.Database.CloseConnection(); }
    }
}
