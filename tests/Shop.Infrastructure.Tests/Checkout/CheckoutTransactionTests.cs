using Microsoft.EntityFrameworkCore;
using Shop.Application.Checkout;
using Shop.Application.Common;
using Shop.Infrastructure.Checkout;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.BLL.Email;
using Xunit;
namespace Shop.Infrastructure.Tests.Checkout;

public class CheckoutTransactionTests
{
    [Fact]
    public async Task Enqueue_failure_rolls_back_order_receipt_and_queue_entry()
    {
        var builder = new DbContextOptionsBuilder<ShopContext>(); var pg = Environment.GetEnvironmentVariable("SHOP_TEST_POSTGRES");
        if (SqlServerTestDatabase.Configure(builder)) { }
        else if (string.IsNullOrWhiteSpace(pg)) builder.UseSqlite("Data Source=" + Path.Combine(Path.GetTempPath(), "shop-refactor-" + Guid.NewGuid().ToString("N") + ".db"));
        else builder.UseNpgsql(new Npgsql.NpgsqlConnectionStringBuilder(pg) { Database = "shop_refactor_" + Guid.NewGuid().ToString("N") }.ConnectionString);
        var options = builder.Options; int productId, areaId; var key = Guid.NewGuid();
        await using (var db = new ShopContext(options))
        {
            if (db.Database.IsSqlServer()) { await db.Database.EnsureCreatedAsync(); SqlServerSchema.Apply(db, true); } else if (db.Database.IsNpgsql()) await db.Database.MigrateAsync(); else { await db.Database.EnsureCreatedAsync(); SqliteSchemaUpgrade.Apply(db, true); }
            var category = await db.Categories.FirstAsync(); var currency = (await db.StoreSettings.FirstAsync()).CurrencyCode;
            var product = new Product { Name = "Atomic checkout", Slug = "atomic-checkout", CategoryId = category.Id, BasePrice = 10, StockQuantity = 10, IsActive = true };
            var gov = new Governorate { Name = "Atomic region", IsActive = true }; db.Products.Add(product); db.Governorates.Add(gov); await db.SaveChangesAsync(); productId = product.Id;
            var area = new DeliveryArea { Name = "Atomic area", GovernorateId = gov.Id, CurrencyCode = currency, ShippingFee = 1, IsActive = true }; db.DeliveryAreas.Add(area); await db.SaveChangesAsync(); areaId = area.Id;
        }
        await using (var db = new ShopContext(options))
        {
            var service = new CheckoutService(new EfCheckoutStore(db, new FailingMail(db)), new Boundary(), new Clock());
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PlaceAsync(new() { IdempotencyKey = key, AreaId = areaId, GuestName = "Test", GuestEmail = "test@example.test", Lines = new() { new() { ProductId = productId, Quantity = 1 } } }, new(false, false, false, null), "hash"));
        }
        await using (var db = new ShopContext(options))
        {
            Assert.False(await db.Orders.AnyAsync(o => o.OrderNumber == "ATOMIC-TEST"));
            await db.Database.OpenConnectionAsync(); using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM \"CheckoutReceipts\" WHERE \"Key\"='" + key.ToString("N") + "'"; Assert.Equal(0L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
            cmd.CommandText = "SELECT COUNT(*) FROM \"EmailOutbox\" WHERE \"Id\"='atomic-test'"; Assert.Equal(0L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
            Assert.Equal(10, (await db.Products.FindAsync(productId))!.StockQuantity);
        }
    }
    private sealed class Clock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
    private sealed class Boundary : ICheckoutBoundary
    {
        public bool DiscountsAllowed => true; public string CreateTrackingProof(string n) => "proof"; public string NewOrderNumber(DateTime n) => "ATOMIC-TEST"; public string EncodeReceipt(CheckoutConfirmation c, string? m) => "test-only-receipt";
    }
    private sealed class FailingMail : IEmailService
    {
        private readonly ShopContext _db; public FailingMail(ShopContext db) => _db = db;
        public Task SendAsync(string to, string subject, string body) => Task.CompletedTask;
        public Task SendStatusUpdateAsync(Order o, StoreSettings s) => Task.CompletedTask;
        public async Task SendOrderSummaryAsync(Order o, StoreSettings s)
        {
            await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"EmailOutbox\" (\"Id\",\"Recipient\",\"Subject\",\"Payload\",\"FileName\",\"State\",\"Attempts\",\"CreatedAtUtc\",\"NextAttemptUtc\",\"ExpiresAtUtc\",\"Lease\",\"LeaseUntilUtc\",\"LastError\",\"DeliveredAtUtc\") VALUES ('atomic-test','test@example.test','test','test','','Pending',0,'','','','','','','')");
            throw new InvalidOperationException("Injected after outbox insert");
        }
    }
}
