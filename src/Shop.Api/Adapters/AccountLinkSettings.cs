using Shop.Application.Accounts;
using Shop.Api.Tenancy;
namespace Shop.Api.Adapters;

public sealed class AccountLinkSettings : IAccountLinkSettings
{
    private readonly IConfiguration _config; private readonly StoreScope _scope; private readonly IWebHostEnvironment _env; private readonly IHttpContextAccessor _http;
    public AccountLinkSettings(IConfiguration config, StoreScope scope, IWebHostEnvironment env, IHttpContextAccessor http) { _config = config; _scope = scope; _env = env; _http = http; }
    public string StoreHost => _scope.Store!.Host;
    public string StoreName => _scope.Store!.Name;
    public bool AllowHttp => _env.IsDevelopment();
    public string Origin => _config[$"StoreLinks:{_scope.StoreId}:Origin"] ?? $"{(AllowHttp ? "http" : "https")}://{StoreHost}{(AllowHttp ? ":4200" : "")}";
    public bool Arabic => _http.HttpContext?.Request.Headers.AcceptLanguage.ToString().Split(',')[0].Split(';')[0].Split('-')[0].Trim().ToLowerInvariant() == "ar";
}
