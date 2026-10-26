using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.DAL.Data;
namespace Shop.DAL.Migrations;
/// <summary>Merchant-managed storefront content.</summary>
[DbContext(typeof(ShopContext)),Migration("20261026050000_ManagedStorefront")]
public sealed class ManagedStorefront:Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder builder)=>builder.Sql(StorefrontSchema.Sql);
    /// <inheritdoc />
    protected override void Down(MigrationBuilder builder){builder.DropTable("StorefrontContent");builder.DropTable("StorefrontSubscribers");}
}
