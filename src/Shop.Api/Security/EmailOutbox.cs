using Shop.Application.Notifications;
using Shop.BLL.Email;
namespace Shop.Api.Security;
// Keep current merchant/worker/BLL callers while persistence resides in Infrastructure.
public sealed class EmailOutbox : IEmailOutbox
{
    private readonly IEmailQueue _queue;
    public EmailOutbox(IEmailQueue queue) => _queue = queue;
    public Task EnqueueAsync(string to, string subject, string body, string? fileName = null, DateTime? expiresAtUtc = null) => _queue.EnqueueAsync(to, subject, body, fileName, expiresAtUtc);
    public Task<List<OutboxEntry>> ReadAsync(bool dueOnly = false) => _queue.ReadAsync(dueOnly);
    public Task DeliverAsync(OutboxEntry entry, EmailService transport) => _queue.DeliverAsync(entry, new Transport(transport));
    private sealed class Transport : IEmailTransport
    {
        private readonly EmailService _transport; public Transport(EmailService transport) => _transport = transport;
        public Task<string> DeliverAsync(string to, string subject, string body, string? fileName) => _transport.DeliverAsync(to, subject, body, fileName);
    }
}
