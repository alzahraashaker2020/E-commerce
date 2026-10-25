using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Shop.DAL.Models;

namespace Shop.BLL.Email;

/// <summary>
/// Default <see cref="IEmailService"/>. Composes the message bodies and either
/// delivers them over SMTP (when <see cref="EmailOptions.Enabled"/> and a host
/// are configured) or, in demo mode, logs the full message and appends a copy
/// to <c>&lt;ContentRootPath&gt;/sent-emails/&lt;orderNumber&gt;.txt</c>. Every send
/// is wrapped in try/catch that logs and swallows, so an email failure never
/// propagates to the caller.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Creates a new <see cref="EmailService"/>.
    /// </summary>
    /// <param name="options">Email configuration.</param>
    /// <param name="logger">Logger used for demo-mode output and errors.</param>
    public EmailService(EmailOptions options, ILogger<EmailService> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(string to, string subject, string body)
    {
        return DispatchAsync(to, subject, body, null);
    }

    /// <inheritdoc />
    public Task SendOrderSummaryAsync(Order order, StoreSettings settings)
    {
        if (settings.CurrencyCode != order.CurrencyCode) settings = new StoreSettings { CurrencyCode = order.CurrencyCode, CurrencySymbol = order.CurrencyCode + " " };
        string to = RecipientFor(order);
        string subject = $"Order {order.OrderNumber} confirmation";
        string body = BuildSummaryBody(order, settings);
        return DispatchAsync(to, subject, body, order.OrderNumber);
    }

    /// <inheritdoc />
    public Task SendStatusUpdateAsync(Order order, StoreSettings settings)
    {
        if (settings.CurrencyCode != order.CurrencyCode) settings = new StoreSettings { CurrencyCode = order.CurrencyCode, CurrencySymbol = order.CurrencyCode + " " };
        string to = RecipientFor(order);
        string subject = $"Order {order.OrderNumber} status update: {order.StatusDefinition?.Name ?? order.Status.ToString()}";
        var sb = new StringBuilder();
        sb.AppendLine($"Order {order.OrderNumber}");
        sb.AppendLine($"Status: {order.StatusDefinition?.Name ?? order.Status.ToString()}");
        sb.AppendLine($"Total: {FormatMoney(order.Total, settings)}");
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
    private static string BuildSummaryBody(Order order, StoreSettings settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Order {order.OrderNumber}");
        sb.AppendLine($"Status: {order.StatusDefinition?.Name ?? order.Status.ToString()}");
        sb.AppendLine();
        sb.AppendLine("Items:");
        foreach (OrderItem item in order.Items)
        {
            sb.AppendLine($"  {item.ProductNameSnapshot} x{item.Quantity} - {FormatMoney(item.LineTotal, settings)}");
        }

        sb.AppendLine();
        sb.AppendLine($"Subtotal: {FormatMoney(order.Subtotal, settings)}");
        sb.AppendLine($"Discount: {FormatMoney(order.DiscountTotal, settings)}");
        sb.AppendLine($"Shipping: {FormatMoney(order.ShippingTotal, settings)}");
        sb.AppendLine($"Tax: {FormatMoney(order.Tax, settings)}");
        sb.AppendLine($"Total: {FormatMoney(order.Total, settings)}");
        sb.AppendLine();
        sb.AppendLine("Shipping address:");
        sb.AppendLine($"  {order.AddressLine1}");
        if (!string.IsNullOrWhiteSpace(order.AddressLine2))
        {
            sb.AppendLine($"  {order.AddressLine2}");
        }

        sb.AppendLine($"  {order.City}, {order.StateRegion} {order.PostalCode}");
        sb.AppendLine($"  {order.Country}");
        sb.AppendLine($"  Phone: {order.ContactPhone}");
        sb.AppendLine();
        sb.AppendLine($"Payment method: {order.PaymentMethod}");
        return sb.ToString();
    }

    // Single dispatch path for every send. SMTP when enabled/configured,
    // otherwise demo mode (log + disk). Always try/catch to never throw.
    private async Task DispatchAsync(string to, string subject, string body, string? orderNumber)
    {
        try
        {
            if (_options.Enabled && !string.IsNullOrWhiteSpace(_options.SmtpHost))
            {
                await SendViaSmtpAsync(to, subject, body);
            }
            else
            {
                WriteDemo(to, subject, body, orderNumber);
            }
        }
        catch (Exception ex)
        {
            // Swallow: an email failure must never break the caller's flow.
            _logger.LogError(ex, "Failed to send email to {Recipient} (subject {Subject})", to, subject);
        }
    }

    // Delivers a message over SMTP using the BCL SmtpClient (no new package).
    private async Task SendViaSmtpAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        using var message = new MailMessage(_options.From, to, subject, body);
        await client.SendMailAsync(message);
    }

    // Demo output: log the full message and append a copy to a per-order file.
    private void WriteDemo(string to, string subject, string body, string? orderNumber)
    {
        _logger.LogInformation(
            "DEMO EMAIL\nTo: {To}\nSubject: {Subject}\nBody:\n{Body}",
            to,
            subject,
            body);

        if (string.IsNullOrWhiteSpace(_options.ContentRootPath))
        {
            return;
        }

        string dir = Path.Combine(_options.ContentRootPath, "sent-emails");
        Directory.CreateDirectory(dir);

        string fileName = string.IsNullOrWhiteSpace(orderNumber) ? "general" : orderNumber;
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
