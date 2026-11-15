using Shop.Application.Notifications;
namespace Shop.Infrastructure.Notifications;

public sealed class EmailPayloadProtector : IEmailPayloadProtector
{
    private readonly Func<string, string> _protect, _unprotect;
    public EmailPayloadProtector(Func<string, string> protect, Func<string, string> unprotect) { _protect = protect; _unprotect = unprotect; }
    public string Protect(string text) => _protect(text);
    public string Unprotect(string text) => _unprotect(text);
}
