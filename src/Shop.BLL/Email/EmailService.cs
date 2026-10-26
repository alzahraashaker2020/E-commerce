using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Shop.DAL.Models;

namespace Shop.BLL.Email;

/// <summary>
/// Default <see cref="IEmailService"/>. Composes the message bodies and either
/// delivers them over SMTP (when <see cref="EmailOptions.Enabled"/> and a host
/// are configured) or, in development, writes a private message copy
/// under <c>&lt;ContentRootPath&gt;/sent-emails</c> without logging its body.
/// Application sends enqueue messages; the worker invokes the transport separately.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private string L(string english,string arabic)=>_options.Language=="ar"?arabic:english;
    private string Content(string text)
    {
        if(_options.Language!="ar")return text;
        if(_options.Translations.TryGetValue(text,out var translated))return translated;
        int split=text.LastIndexOf(" (",StringComparison.Ordinal);
        if(split>0&&text.EndsWith(')')&&_options.Translations.TryGetValue(text[..split],out var name))
            return name+" ("+string.Join("، ",text[(split+2)..^1].Split(", ").Select(part=>_options.Translations.TryGetValue(part,out var value)?value:part))+")";
        return text;
    }
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailOutbox? _outbox;

    /// <summary>
    /// Creates a new <see cref="EmailService"/>.
    /// </summary>
    /// <param name="options">Email configuration.</param>
    /// <param name="logger">Logger used for demo-mode output and errors.</param>
    /// <param name="outbox">Durable store queue used by application sends.</param>
    public EmailService(EmailOptions options, ILogger<EmailService> logger, IEmailOutbox? outbox = null)
    {
        _options = options;
        _logger = logger;
        _outbox = outbox;
    }

    /// <inheritdoc />
    public Task SendAsync(string to, string subject, string body)
    {
        return DispatchAsync(to, subject, body, null);
    }

    /// <inheritdoc />
    public async Task SendOrderSummaryAsync(Order order, StoreSettings settings)
    {
        if (settings.CurrencyCode != order.CurrencyCode) settings = new StoreSettings { CurrencyCode = order.CurrencyCode, CurrencySymbol = order.CurrencyCode + " " };
        string to = RecipientFor(order);
        string subject = _options.Language=="ar"?$"تأكيد الطلب {order.OrderNumber}":$"Order {order.OrderNumber} confirmation";
        string body = BuildSummaryBody(order, settings);
        if (_options.CustomerOrdersEnabled) await DispatchAsync(to, subject, body, order.OrderNumber);
        if (_options.MerchantOrdersEnabled && !string.IsNullOrWhiteSpace(_options.MerchantEmail) && !string.Equals(to,_options.MerchantEmail,StringComparison.OrdinalIgnoreCase))
            await DispatchAsync(_options.MerchantEmail, (_options.Language=="ar"?$"طلب جديد {order.OrderNumber}":$"New order {order.OrderNumber}"), body, "merchant-"+order.OrderNumber);
    }

    /// <inheritdoc />
    public Task SendStatusUpdateAsync(Order order, StoreSettings settings)
    {
        if (!_options.CustomerStatusEnabled) return Task.CompletedTask;
        if (settings.CurrencyCode != order.CurrencyCode) settings = new StoreSettings { CurrencyCode = order.CurrencyCode, CurrencySymbol = order.CurrencyCode + " " };
        string to = RecipientFor(order);
        string subject = $"{L("Order","الطلب")} {order.OrderNumber} {L("status update:","تحديث الحالة:")} {Content(order.StatusDefinition?.Name ?? order.Status.ToString())}";
        var sb = new StringBuilder();
        sb.AppendLine($"{L("Order","الطلب")} {order.OrderNumber}");
        sb.AppendLine($"{L("Status:","الحالة:")} {Content(order.StatusDefinition?.Name ?? order.Status.ToString())}");
        sb.AppendLine($"{L("Total:","الإجمالي:")} {FormatMoney(order.Total, settings)}");
        return DispatchAsync(to, subject, sb.ToString(), order.OrderNumber);
    }

    // Resolves the best recipient address for an order (customer email falls
    // back to the guest email, then to an empty string in demo mode).
    private static string RecipientFor(Order order)
    {
        if (order.Customer is not null && !string.IsNullOrWhiteSpace(order.Customer.Email))
        {
            return order.Customer.Email;
        }

        return order.GuestEmail ?? string.Empty;
    }

    // Formats a money amount with the store currency symbol.
    private static string FormatMoney(decimal amount, StoreSettings settings)
    {
        return $"{settings.CurrencySymbol}{amount:0.00}";
    }

    // Composes the full order-summary body: order number, line items, totals,
    // shipping address, payment method, and status.
    private string BuildSummaryBody(Order order, StoreSettings settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{L("Order","الطلب")} {order.OrderNumber}");
        sb.AppendLine($"{L("Status:","الحالة:")} {Content(order.StatusDefinition?.Name ?? order.Status.ToString())}");
        sb.AppendLine();
        sb.AppendLine(L("Items:","المنتجات:"));
        foreach (OrderItem item in order.Items)
        {
            sb.AppendLine($"  {Content(item.ProductNameSnapshot)} x{item.Quantity} - {FormatMoney(item.LineTotal, settings)}");
        }

        sb.AppendLine();
        sb.AppendLine($"{L("Subtotal:","الإجمالي الفرعي:")} {FormatMoney(order.Subtotal, settings)}");
        sb.AppendLine($"{L("Discount:","الخصم:")} {FormatMoney(order.DiscountTotal, settings)}");
        sb.AppendLine($"{L("Shipping:","الشحن:")} {FormatMoney(order.ShippingTotal, settings)}");
        sb.AppendLine($"{L("Tax:","الضريبة:")} {FormatMoney(order.Tax, settings)}");
        sb.AppendLine($"{L("Total:","الإجمالي:")} {FormatMoney(order.Total, settings)}");
        sb.AppendLine();
        sb.AppendLine(L("Shipping address:","عنوان الشحن:"));
        sb.AppendLine($"  {order.AddressLine1}");
        if (!string.IsNullOrWhiteSpace(order.AddressLine2))
        {
            sb.AppendLine($"  {order.AddressLine2}");
        }

        sb.AppendLine($"  {order.City}, {order.StateRegion} {order.PostalCode}");
        sb.AppendLine($"  {order.Country}");
        sb.AppendLine($"  {L("Phone:","الهاتف:")} {order.ContactPhone}");
        sb.AppendLine();
        sb.AppendLine($"{L("Payment method:","طريقة الدفع:")} {(order.PaymentMethod==PaymentMethod.Cash?L("Cash on delivery","الدفع عند الاستلام"):L("Card","بطاقة"))}");
        return sb.ToString();
    }

    // Persist before acknowledging the business operation; network delivery is separate.
    private async Task DispatchAsync(string to, string subject, string body, string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(to)) return;
        if (_outbox is not null) { await _outbox.EnqueueAsync(to, subject, body, orderNumber); return; }
        await DeliverAsync(to, subject, body, orderNumber);
    }

    /// <summary>Worker-only transport. Failures propagate so they can be retried.</summary>
    public async Task<string> DeliverAsync(string to, string subject, string body, string? fileName)
    {
            if (_options.Enabled)
            {
                if(string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.SmtpUser) || string.IsNullOrWhiteSpace(_options.SmtpPassword) || string.IsNullOrWhiteSpace(_options.From))
                    throw new InvalidOperationException("SMTP configuration is incomplete.");
                await SendViaSmtpAsync(to, subject, body);
                return "Sent";
            }
            else if (_options.AllowDevelopmentSink)
            {
                WriteDemo(to, subject, body, fileName);
                return "DevelopmentOnly";
            }
            throw new InvalidOperationException("Email delivery is not configured.");
    }

    // Delivers a message over SMTP using the BCL SmtpClient (no new package).
    private async Task SendViaSmtpAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        using var message = new MailMessage(_options.From, to, subject, body){SubjectEncoding=Encoding.UTF8,BodyEncoding=Encoding.UTF8};
        await client.SendMailAsync(message).WaitAsync(TimeSpan.FromSeconds(15));
    }

    // Development-only private copy, without logging message bodies.
    private void WriteDemo(string to, string subject, string body, string? orderNumber)
    {
        _logger.LogInformation("Development email written to the private store message directory.");

        if (string.IsNullOrWhiteSpace(_options.ContentRootPath))
        {
            return;
        }

        string dir = Path.Combine(_options.ContentRootPath, "sent-emails");
        Directory.CreateDirectory(dir);

        string fileName = string.IsNullOrWhiteSpace(orderNumber) ? "message-"+Guid.NewGuid().ToString("N") : orderNumber;
        string path = Path.Combine(dir, $"{fileName}.txt");

        var sb = new StringBuilder();
        sb.AppendLine($"To: {to}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine("Body:");
        sb.AppendLine(body);
        sb.AppendLine("----");

        File.AppendAllText(path, sb.ToString());
    }
}
