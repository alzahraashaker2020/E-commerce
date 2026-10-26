using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.DAL.Data;

namespace Shop.DAL.Migrations;

/// <summary>Durable per-store email messages and attempt history.</summary>
[DbContext(typeof(ShopContext)), Migration("20261026030000_DurableEmailOutbox")]
public sealed class DurableEmailOutbox : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder builder) => builder.Sql(EmailOutboxSchema.Sql);
    /// <inheritdoc />
    protected override void Down(MigrationBuilder builder) { builder.DropTable("EmailAttempts"); builder.DropTable("EmailOutbox"); }
}
