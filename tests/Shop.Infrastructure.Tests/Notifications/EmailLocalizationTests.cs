using Microsoft.Extensions.Logging.Abstractions;
using Shop.Infrastructure.Notifications;
using Shop.Application.Notifications;
using Shop.BLL.Email;
using Shop.DAL.Models;
using Xunit;
namespace Shop.Infrastructure.Tests.Notifications;

public class EmailLocalizationTests
{
    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task First_send_awaits_store_content_once_and_preserves_language(string language)
    {
        var options = new EmailOptions { Language = language, CustomerOrdersEnabled = true, CustomerStatusEnabled = true, MerchantOrdersEnabled = false }; var queue = new Queue(); var content = new Content();
        var composer = new StoreEmailComposer(options, new EmailService(options, NullLogger<EmailService>.Instance, queue), content); Assert.Equal(0, content.Calls);
        var order = new Order { OrderNumber = "MAIL-TEST", GuestName = "Private Customer", GuestEmail = "test@example.test", CurrencyCode = "USD", Total = 10, Status = OrderStatus.Confirmed, Items = new List<OrderItem> { new() { ProductNameSnapshot = "Cotton set (Small, Red)", Quantity = 1, UnitPrice = 10, LineTotal = 10 } } };
        await composer.SendOrderSummaryAsync(order, new() { CurrencyCode = "USD", CurrencySymbol = "$" }); await composer.SendStatusUpdateAsync(order, new() { CurrencyCode = "USD", CurrencySymbol = "$" });
        Assert.Equal(1, content.Calls); Assert.Equal(2, queue.Bodies.Count);
        Assert.Contains(language == "ar" ? "طقم قطن (صغير، أحمر)" : "Cotton set (Small, Red)", queue.Bodies[0]);
    }
    private sealed class Content : IStoreEmailContent
    {
        public int Calls; public async Task<Dictionary<string, string>> ReadTranslationsAsync() { Calls++; await Task.Yield(); return new() { { "Cotton set", "طقم قطن" }, { "Small", "صغير" }, { "Red", "أحمر" } }; }
    }
    private sealed class Queue : IEmailOutbox
    {
        public List<string> Bodies = new(); public Task EnqueueAsync(string to, string subject, string body, string? fileName = null, DateTime? expiresAtUtc = null) { Bodies.Add(body); return Task.CompletedTask; }
    }
}
