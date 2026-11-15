namespace Shop.Application.Notifications;

public sealed record OutboxEntry(string Id, string Recipient, string Subject, string State, int Attempts, string CreatedAtUtc, string NextAttemptUtc, string LastError, string DeliveredAtUtc);
public interface IEmailQueue
{
    Task EnqueueAsync(string to, string subject, string body, string? fileName = null, DateTime? expiresAtUtc = null);
    Task<List<OutboxEntry>> ReadAsync(bool dueOnly = false);
    Task DeliverAsync(OutboxEntry entry, IEmailTransport transport);
}
public interface IEmailTransport { Task<string> DeliverAsync(string to, string subject, string body, string? fileName); }
public interface IEmailPayloadProtector { string Protect(string text); string Unprotect(string text); }
public interface IOutboxFailureJournal { Task RecordFailureAsync(string messageId); }
