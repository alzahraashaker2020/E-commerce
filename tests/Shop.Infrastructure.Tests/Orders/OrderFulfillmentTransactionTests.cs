using Microsoft.EntityFrameworkCore;
using Shop.Application.Orders;
using Shop.Infrastructure.Orders;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.BLL.Email;
using Xunit;
namespace Shop.Infrastructure.Tests.Orders;

public class OrderFulfillmentTransactionTests
{
    private static DbContextOptions<ShopContext> Options()
    {
        var builder = new DbContextOptionsBuilder<ShopContext>();
        var pg = Environment.GetEnvironmentVariable("SHOP_TEST_POSTGRES");
        if (SqlServerTestDatabase.Configure(builder)) { }
        else if (!string.IsNullOrWhiteSpace(pg))
        {
            var connection = new Npgsql.NpgsqlConnectionStringBuilder(pg) { Database = "shop_refactor_" + Guid.NewGuid().ToString("N") };
            builder.UseNpgsql(connection.ConnectionString);
        }
        else builder.UseSqlite("Data Source=" + Path.Combine(Path.GetTempPath(), "shop-refactor-" + Guid.NewGuid().ToString("N") + ".db"));
        return builder.Options;
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Later_stock_failure_or_notification_failure_rolls_back_entire_transition(bool failEmail)
    {
        var options = Options(); int orderId, productId, definitionId;
        await using (var db = new ShopContext(options))
        {
            if (db.Database.IsSqlServer()) { await db.Database.EnsureCreatedAsync(); SqlServerSchema.Apply(db, true); } else if (db.Database.IsNpgsql()) await db.Database.MigrateAsync(); else { await db.Database.EnsureCreatedAsync(); SqliteSchemaUpgrade.Apply(db, true); }
            var category = await db.Categories.FirstAsync();
            var first = new Product { Name = "Rollback one", Slug = "rollback-one", CategoryId = category.Id, BasePrice = 10, StockQuantity = 10, IsActive = true };
            var second = new Product { Name = "Rollback two", Slug = "rollback-two", CategoryId = category.Id, BasePrice = 10, StockQuantity = 0, IsActive = true };
            db.Products.AddRange(first, second); await db.SaveChangesAsync(); productId = first.Id;
            var pending = await db.OrderStatusDefinitions.FirstAsync(s => s.IsDefault); definitionId = await db.OrderStatusDefinitions.Where(s => s.Behavior == OrderStatus.Confirmed && s.IsActive).Select(s => s.Id).FirstAsync();
            var order = new Order { OrderNumber = "ROLL-" + Guid.NewGuid().ToString("N")[..12], Status = OrderStatus.Pending, StatusDefinitionId = pending.Id, Items = new List<OrderItem> { new() { ProductId = first.Id, Quantity = 2, UnitPrice = 10, LineTotal = 20 } } };
            if (!failEmail) order.Items.Add(new() { ProductId = second.Id, Quantity = 1, UnitPrice = 10, LineTotal = 10 });
            db.Orders.Add(order); await db.SaveChangesAsync(); orderId = order.Id;
        }
        await using (var db = new ShopContext(options))
        {
            var service = new OrderFulfillmentService(new EfOrderFulfillmentStore(db, new Mail(failEmail)));
            if (failEmail) await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangeStatusAsync(new(orderId, definitionId, null)));
            else Assert.Equal(FulfillmentOutcomeKind.Conflict, (await service.ChangeStatusAsync(new(orderId, definitionId, null))).Kind);
        }
        await using (var verify = new ShopContext(options))
        {
            Assert.Equal(10, (await verify.Products.FindAsync(productId))!.StockQuantity);
            var order = (await verify.Orders.FindAsync(orderId))!; Assert.False(order.StockDeducted); Assert.Equal(OrderStatus.Pending, order.Status);
        }
    }
    private sealed class Mail : IEmailService
    {
        private readonly bool _fail; public Mail(bool fail) => _fail = fail;
        public Task SendAsync(string to, string subject, string body) => Task.CompletedTask;
        public Task SendOrderSummaryAsync(Order order, StoreSettings settings) => Task.CompletedTask;
        public Task SendStatusUpdateAsync(Order order, StoreSettings settings) => _fail ? Task.FromException(new InvalidOperationException("Injected notification failure")) : Task.CompletedTask;
    }
}
