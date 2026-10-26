using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.DAL.Data;

namespace Shop.DAL.Migrations;

/// <summary>Single-use customer actions and per-store email preferences.</summary>
[DbContext(typeof(ShopContext)), Migration("20261026020000_CustomerEmailSecurity")]
public sealed class CustomerEmailSecurity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder builder) => builder.Sql("""
        CREATE TABLE "CustomerActions" ("TokenHash" text PRIMARY KEY, "CustomerId" integer NOT NULL REFERENCES "Customers"("Id"), "Purpose" text NOT NULL, "ExpiresAtUtc" text NOT NULL, "CredentialStamp" text NOT NULL);
        CREATE INDEX "IX_CustomerActions_CustomerId" ON "CustomerActions"("CustomerId");
        CREATE TABLE "VerifiedCustomers" ("CustomerId" integer PRIMARY KEY REFERENCES "Customers"("Id"), "Email" text NOT NULL);
        CREATE TABLE "StoreEmailPreferences" ("Id" integer PRIMARY KEY, "MerchantEmail" text NOT NULL, "CustomerOrdersEnabled" boolean NOT NULL, "CustomerStatusEnabled" boolean NOT NULL, "MerchantOrdersEnabled" boolean NOT NULL);
        INSERT INTO "StoreEmailPreferences" VALUES (1,'',true,true,true);
        """);
    /// <inheritdoc />
    protected override void Down(MigrationBuilder builder) { builder.DropTable("CustomerActions"); builder.DropTable("VerifiedCustomers"); builder.DropTable("StoreEmailPreferences"); }
}
