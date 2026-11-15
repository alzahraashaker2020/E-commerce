using Microsoft.EntityFrameworkCore;
using Shop.Application.Returns;
using Shop.Application.Common;
using Shop.Infrastructure.Returns;
using Shop.BLL.Email;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Xunit;
namespace Shop.Infrastructure.Tests.Returns;

public class ReturnAtomicityTests
{
    [Fact]
    public async Task Failed_notification_rolls_back_return_version_audit_and_restock()
    {
        var builder = new DbContextOptionsBuilder<ShopContext>(); var pg = Environment.GetEnvironmentVariable("SHOP_TEST_POSTGRES");
        if (SqlServerTestDatabase.Configure(builder)) { }
        else if (string.IsNullOrWhiteSpace(pg)) builder.UseSqlite("Data Source=" + Path.Combine(Path.GetTempPath(), "shop-refactor-" + Guid.NewGuid().ToString("N") + ".db"));
        else builder.UseNpgsql(new Npgsql.NpgsqlConnectionStringBuilder(pg) { Database = "shop_refactor_" + Guid.NewGuid().ToString("N") }.ConnectionString);
        var options = builder.Options; int orderId, itemId, productId; string returnId;
        await using (var db = new ShopContext(options))
        {
            if (db.Database.IsSqlServer()) { await db.Database.EnsureCreatedAsync(); SqlServerSchema.Apply(db, true); } else if (db.Database.IsNpgsql()) await db.Database.MigrateAsync(); else { await db.Database.EnsureCreatedAsync(); SqliteSchemaUpgrade.Apply(db, true); }
            var product = new Product { Name = "Return rollback", Slug = "return-rollback", CategoryId = (await db.Categories.FirstAsync()).Id, StockQuantity = 8, IsActive = true, BasePrice = 10 }; db.Products.Add(product); await db.SaveChangesAsync(); productId = product.Id;
            var order = new Order { OrderNumber = "RETURN-TEST", Status = OrderStatus.Delivered, StockDeducted = true, Total = 20, Subtotal = 20, StatusDefinitionId = await db.OrderStatusDefinitions.Where(s => s.Behavior == OrderStatus.Delivered).Select(s => s.Id).FirstAsync(), Items = new List<OrderItem> { new() { ProductId = product.Id, Quantity = 2, UnitPrice = 10, LineTotal = 20 } } }; db.Orders.Add(order); await db.SaveChangesAsync(); orderId = order.Id; itemId = order.Items.Single().Id;
            var service = new ReturnService(new SqlReturnStore(db, new Queue(false)), new Clock());
            var entry = await service.RequestAsync(orderId, Guid.NewGuid(), itemId, 1, "test reason"); returnId = entry.Id;
            await service.ChangeAsync(entry.Id, entry.Version, "Approve", "approved", false, 0, "", "merchant");
        }
        await using (var db = new ShopContext(options))
        {
            var service = new ReturnService(new SqlReturnStore(db, new Queue(true)), new Clock());
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangeAsync(returnId, 2, "Receive", "inspection", true, 0, "", "merchant"));
        }
        await using (var db = new ShopContext(options))
        {
            Assert.Equal(8, (await db.Products.FindAsync(productId))!.StockQuantity);
            var entries = await new SqlReturnStore(db, new Queue(false)).ReadAsync(orderId, default); var entry = Assert.Single(entries); Assert.Equal("Approved", entry.State); Assert.Equal(2, entry.Version); Assert.False(entry.Restocked);
            await db.Database.OpenConnectionAsync(); using var cmd = db.Database.GetDbConnection().CreateCommand(); cmd.CommandText = "SELECT COUNT(*) FROM \"ReturnEvents\" WHERE \"ReturnId\"=@id"; var parameter = cmd.CreateParameter(); parameter.ParameterName = "id"; parameter.Value = returnId; cmd.Parameters.Add(parameter); Assert.Equal(2L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
        }
    }
    private sealed class Clock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
    private sealed class Queue : IEmailOutbox
    {
        private readonly bool _fail; public Queue(bool fail) => _fail = fail;
        public Task EnqueueAsync(string to, string subject, string body, string? fileName = null, DateTime? expiresAtUtc = null) => _fail ? Task.FromException(new InvalidOperationException("Injected queue failure")) : Task.CompletedTask;
    }
}
