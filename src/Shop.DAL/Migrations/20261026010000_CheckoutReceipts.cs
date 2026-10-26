using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.DAL.Data;

namespace Shop.DAL.Migrations;

/// <summary>Durable checkout attempt results committed atomically with the order.</summary>
[DbContext(typeof(ShopContext)), Migration("20261026010000_CheckoutReceipts")]
public sealed class CheckoutReceipts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder builder) => builder.Sql("CREATE TABLE \"CheckoutReceipts\" (\"Key\" text PRIMARY KEY, \"Owner\" text NOT NULL, \"RequestHash\" text NOT NULL, \"ResponseJson\" text NOT NULL);");
    /// <inheritdoc />
    protected override void Down(MigrationBuilder builder) => builder.DropTable("CheckoutReceipts");
}
