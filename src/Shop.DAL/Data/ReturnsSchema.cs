namespace Shop.DAL.Data;

/// <summary>Store-isolated return requests and their immutable event history.</summary>
public static class ReturnsSchema
{
    /// <summary>Portable schema shared by SQLite upgrades and PostgreSQL migrations.</summary>
    public const string Sql="""
        CREATE TABLE "ReturnRequests" (
          "Id" text PRIMARY KEY, "OrderId" integer NOT NULL REFERENCES "Orders"("Id"),
          "OrderItemId" integer NOT NULL REFERENCES "OrderItems"("Id"), "Quantity" integer NOT NULL,
          "Reason" text NOT NULL, "State" text NOT NULL, "Restocked" integer NOT NULL,
          "MaxRefund" text NOT NULL, "RefundAmount" text NOT NULL, "RefundReference" text NOT NULL,
          "Notes" text NOT NULL, "Version" integer NOT NULL, "CreatedAtUtc" text NOT NULL);
        CREATE INDEX "IX_ReturnRequests_Order" ON "ReturnRequests" ("OrderId", "OrderItemId");
        CREATE TABLE "ReturnEvents" ("Id" text PRIMARY KEY, "ReturnId" text NOT NULL REFERENCES "ReturnRequests"("Id"), "Action" text NOT NULL, "Actor" text NOT NULL, "Notes" text NOT NULL, "CreatedAtUtc" text NOT NULL);
        """;
}
