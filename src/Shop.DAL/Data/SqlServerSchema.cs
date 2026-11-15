using Microsoft.EntityFrameworkCore;

namespace Shop.DAL.Data;

/// <summary>SQL Server's separately versioned supplemental schema; never runs SQLite/PostgreSQL migrations.</summary>
public static class SqlServerSchema
{
    public const int Version = 1;
    public static void Apply(ShopContext db, bool newlyCreated = false)
    {
        if (!db.Database.IsSqlServer()) throw new InvalidOperationException("SQL Server context required.");
        // Existing databases must have been explicitly created/imported by our provisioning tool.
        if (!newlyCreated)
        {
            db.Database.OpenConnection();
            try
            {
                using var cmd = db.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT Version FROM __SqlServerSchema WHERE Id=1";
                if (Convert.ToInt32(cmd.ExecuteScalar()) != Version) throw new InvalidOperationException("Review SQL Server schema version before startup.");
            }
            finally { db.Database.CloseConnection(); }
            return;
        }
        using var tx = db.Database.BeginTransaction();
        db.Database.ExecuteSqlRaw(Sql);
        tx.Commit();
    }
    public const string Sql = """
        CREATE TABLE CheckoutReceipts ([Key] nvarchar(450) COLLATE Latin1_General_100_BIN2 PRIMARY KEY, Owner nvarchar(max) NOT NULL, RequestHash nvarchar(max) NOT NULL, ResponseJson nvarchar(max) NOT NULL);
        CREATE TABLE CustomerActions (TokenHash nvarchar(450) PRIMARY KEY, CustomerId int NOT NULL REFERENCES Customers(Id), Purpose nvarchar(max) NOT NULL, ExpiresAtUtc nvarchar(40) NOT NULL, CredentialStamp nvarchar(max) NOT NULL);
        CREATE INDEX IX_CustomerActions_CustomerId ON CustomerActions(CustomerId);
        CREATE TABLE VerifiedCustomers (CustomerId int PRIMARY KEY REFERENCES Customers(Id), Email nvarchar(256) NOT NULL);
        CREATE TABLE StoreEmailPreferences (Id int PRIMARY KEY, MerchantEmail nvarchar(max) NOT NULL, CustomerOrdersEnabled int NOT NULL, CustomerStatusEnabled int NOT NULL, MerchantOrdersEnabled int NOT NULL);
        INSERT INTO StoreEmailPreferences VALUES (1,'',1,1,1);
        CREATE TABLE EmailOutbox (
          Id nvarchar(450) PRIMARY KEY, Recipient nvarchar(max) NOT NULL, Subject nvarchar(max) NOT NULL,
          Payload nvarchar(max) NOT NULL, FileName nvarchar(max) NOT NULL, State nvarchar(40) NOT NULL,
          Attempts int NOT NULL, CreatedAtUtc nvarchar(40) NOT NULL, NextAttemptUtc nvarchar(40) NOT NULL,
          ExpiresAtUtc nvarchar(40) NOT NULL, Lease nvarchar(450) NOT NULL, LeaseUntilUtc nvarchar(40) NOT NULL,
          LastError nvarchar(max) NOT NULL, DeliveredAtUtc nvarchar(40) NOT NULL);
        CREATE INDEX IX_EmailOutbox_Due ON EmailOutbox(State,NextAttemptUtc);
        CREATE TABLE EmailAttempts (Id nvarchar(450) PRIMARY KEY, MessageId nvarchar(450) NOT NULL REFERENCES EmailOutbox(Id), AttemptedAtUtc nvarchar(40) NOT NULL, Outcome nvarchar(max) NOT NULL);
        CREATE TABLE ReturnRequests (
          Id nvarchar(450) PRIMARY KEY, OrderId int NOT NULL REFERENCES Orders(Id), OrderItemId int NOT NULL REFERENCES OrderItems(Id), Quantity int NOT NULL,
          Reason nvarchar(max) NOT NULL, State nvarchar(40) NOT NULL, Restocked int NOT NULL,
          MaxRefund nvarchar(100) NOT NULL, RefundAmount nvarchar(100) NOT NULL, RefundReference nvarchar(max) NOT NULL,
          Notes nvarchar(max) NOT NULL, Version int NOT NULL, CreatedAtUtc nvarchar(40) NOT NULL);
        CREATE INDEX IX_ReturnRequests_Order ON ReturnRequests(OrderId,OrderItemId);
        CREATE TABLE ReturnEvents (Id nvarchar(450) PRIMARY KEY, ReturnId nvarchar(450) NOT NULL REFERENCES ReturnRequests(Id), Action nvarchar(max) NOT NULL, Actor nvarchar(max) NOT NULL, Notes nvarchar(max) NOT NULL, CreatedAtUtc nvarchar(40) NOT NULL);
        CREATE TABLE StorefrontContent (Id int PRIMARY KEY, Version int NOT NULL, Document nvarchar(max) NOT NULL);
        CREATE TABLE StorefrontSubscribers (Email nvarchar(450) PRIMARY KEY, JoinedAtUtc nvarchar(40) NOT NULL);
        CREATE TABLE __SqlServerSchema (Id int PRIMARY KEY, Version int NOT NULL);
        INSERT INTO __SqlServerSchema VALUES (1,1);
        """;
}
