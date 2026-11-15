using Shop.Api.Tenancy;
using Shop.BLL.Email;
using Shop.DAL.Data;
using Shop.Application.Notifications;
namespace Shop.Api.Composition;

public static class EmailRegistration
{
    public static IServiceCollection AddShopEmail(this IServiceCollection services, IConfiguration config, IWebHostEnvironment environment)
    {
        services.AddScoped<EmailOptions>(sp =>
        {
            var store = sp.GetRequiredService<StoreScope>();
            var options = config.GetSection(store.Store?.ConnectionKey == "DefaultStore" ? "Email" : "StoreEmail:" + store.StoreId).Get<EmailOptions>() ?? new();
            options.AllowDevelopmentSink = environment.IsDevelopment();
            options.Language = sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Headers.AcceptLanguage.ToString().Split(',')[0].Split(';')[0].Split('-')[0].Trim().ToLowerInvariant() == "ar" ? "ar" : "en";
            StoreEmailPreferences.Read(sp.GetRequiredService<ShopContext>(), options);
            options.ContentRootPath = Path.Combine(environment.ContentRootPath, "store-data", store.StoreId); return options;
        });
        services.AddScoped<EmailService>(sp => new(sp.GetRequiredService<EmailOptions>(), sp.GetRequiredService<ILogger<EmailService>>(), sp.GetRequiredService<IEmailOutbox>()));
        services.AddScoped<IStoreEmailContent, Shop.Api.Adapters.StoreEmailContent>();
        services.AddScoped<IEmailService, Shop.Infrastructure.Notifications.StoreEmailComposer>();
        return services;
    }
}
