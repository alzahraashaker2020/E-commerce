using Shop.DAL.Models;

namespace Shop.BLL.Email;

/// <summary>
/// Queues transactional messages in the current store's durable outbox.
/// SMTP delivery and retries occur in a background worker after commit.
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
