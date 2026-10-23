using Shop.DAL.Models;

namespace Shop.BLL.Email;

/// <summary>
/// Sends transactional emails. In demo mode (SMTP disabled) messages are logged
/// and persisted to disk; when SMTP is enabled they are delivered via
/// <see cref="System.Net.Mail.SmtpClient"/>. Every send is best-effort: failures
/// are logged and swallowed so e-mailing never breaks the caller's flow.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a plain message to the given recipient.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Message subject.</param>
    /// <param name="body">Message body.</param>
    Task SendAsync(string to, string subject, string body);

    /// <summary>
    /// Sends an order summary (line items, totals, address, payment) for a
    /// newly placed order.
    /// </summary>
    /// <param name="order">The order to summarise.</param>
    /// <param name="settings">Store settings supplying the currency symbol.</param>
    Task SendOrderSummaryAsync(Order order, StoreSettings settings);

    /// <summary>
    /// Sends an order status-update notification.
    /// </summary>
    /// <param name="order">The order whose status changed.</param>
    /// <param name="settings">Store settings supplying the currency symbol.</param>
    Task SendStatusUpdateAsync(Order order, StoreSettings settings);
}
