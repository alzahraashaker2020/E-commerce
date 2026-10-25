using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.DAL.Data;

namespace Shop.DAL.Migrations;

/// <summary>Adds the durable confirmation stock marker to migrated databases.</summary>
[DbContext(typeof(ShopContext))]
[Migration("20261025210000_OrderStockDeducted")]
public class OrderStockDeducted : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<bool>(name: "StockDeducted", table: "Orders", nullable: false, defaultValue: false);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "StockDeducted", table: "Orders");
}
