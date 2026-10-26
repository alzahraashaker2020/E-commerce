using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.DAL.Data;

namespace Shop.DAL.Migrations;

/// <summary>Returns and inspection/refund audit history.</summary>
[DbContext(typeof(ShopContext)), Migration("20261026040000_OrderReturns")]
public sealed class OrderReturns:Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder builder)=>builder.Sql(ReturnsSchema.Sql);
    /// <inheritdoc />
    protected override void Down(MigrationBuilder builder){builder.DropTable("ReturnEvents");builder.DropTable("ReturnRequests");}
}
