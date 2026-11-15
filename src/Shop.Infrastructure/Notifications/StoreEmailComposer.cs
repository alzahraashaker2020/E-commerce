using Shop.Application.Notifications;
using Shop.BLL.Email;
using Shop.DAL.Models;
namespace Shop.Infrastructure.Notifications;
// Request-scoped composition awaits CMS once; worker transport uses the durable text snapshot.
public sealed class StoreEmailComposer : IEmailService
{
    private readonly EmailOptions _options; private readonly EmailService _transport; private readonly IStoreEmailContent _content; private Task? _loaded;
    public StoreEmailComposer(EmailOptions options, EmailService transport, IStoreEmailContent content) { _options = options; _transport = transport; _content = content; }
    private Task ReadyAsync() => _loaded ??= LoadAsync();
    private async Task LoadAsync() => _options.Translations = await _content.ReadTranslationsAsync();
    public async Task SendAsync(string to, string subject, string body) { await ReadyAsync(); await _transport.SendAsync(to, subject, body); }
    public async Task SendOrderSummaryAsync(Order order, StoreSettings settings) { await ReadyAsync(); await _transport.SendOrderSummaryAsync(order, settings); }
    public async Task SendStatusUpdateAsync(Order order, StoreSettings settings) { await ReadyAsync(); await _transport.SendStatusUpdateAsync(order, settings); }
}
