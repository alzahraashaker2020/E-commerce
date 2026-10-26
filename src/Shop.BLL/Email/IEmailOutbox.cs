namespace Shop.BLL.Email;

/// <summary>Persists a message in the current store transaction for later delivery.</summary>
public interface IEmailOutbox
{
    /// <summary>Enqueues an encrypted message with an optional expiry.</summary>
    Task EnqueueAsync(string to, string subject, string body, string? fileName = null, DateTime? expiresAtUtc = null);
}
