using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.DAL.Data;

namespace Shop.DAL.Migrations;

/// <summary>Adds ordered variant galleries.</summary>
[DbContext(typeof(ShopContext))]
[Migration("20261025220000_VariantGallery")]
public class VariantGallery : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "ImageUrls", table: "ProductVariants", nullable: false, defaultValue: "[]");
    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "ImageUrls", table: "ProductVariants");
}
