using Shop.Application.Notifications;
using Shop.Api.Tenancy;
namespace Shop.Api.Adapters;

public sealed class StoreEmailContent : IStoreEmailContent
{
    private readonly StorefrontContent _content; public StoreEmailContent(StorefrontContent content) => _content = content;
    public async Task<Dictionary<string, string>> ReadTranslationsAsync() => (await _content.ReadAsync()).Translations;
}
