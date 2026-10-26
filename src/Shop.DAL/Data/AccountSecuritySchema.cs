namespace Shop.DAL.Data;

/// <summary>Internal account actions and store notification preferences, isolated with the store database.</summary>
public static class AccountSecuritySchema
{
    /// <summary>SQLite additive schema.</summary>
    public const string Sqlite = """
        CREATE TABLE IF NOT EXISTS "CustomerActions" ("TokenHash" TEXT PRIMARY KEY, "CustomerId" INTEGER NOT NULL REFERENCES "Customers"("Id"), "Purpose" TEXT NOT NULL, "ExpiresAtUtc" TEXT NOT NULL, "CredentialStamp" TEXT NOT NULL);
        CREATE INDEX IF NOT EXISTS "IX_CustomerActions_CustomerId" ON "CustomerActions"("CustomerId");
        CREATE TABLE IF NOT EXISTS "VerifiedCustomers" ("CustomerId" INTEGER PRIMARY KEY REFERENCES "Customers"("Id"), "Email" TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS "StoreEmailPreferences" ("Id" INTEGER PRIMARY KEY, "MerchantEmail" TEXT NOT NULL, "CustomerOrdersEnabled" INTEGER NOT NULL, "CustomerStatusEnabled" INTEGER NOT NULL, "MerchantOrdersEnabled" INTEGER NOT NULL);
        INSERT OR IGNORE INTO "StoreEmailPreferences" VALUES (1,'',1,1,1);
        """;
}
