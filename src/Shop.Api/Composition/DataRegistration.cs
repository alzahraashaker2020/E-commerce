using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.DAL.Data;
using Shop.BLL.Email;
using Shop.BLL.IRepositories;
using Shop.BLL.Repositories;
using Shop.BLL.Security;
using Shop.SL.TokenManager;
namespace Shop.Api.Composition;

public static class DataRegistration
{
    public static IServiceCollection AddShopData(this IServiceCollection services, IConfiguration config, IWebHostEnvironment environment)
    {
        // Store databases independently select SqlServer, Postgres or Sqlite.
        // The platform registry has its own explicit provider/connection configuration.
        services.AddDbContext<PlatformContext>(o => PlatformDatabase.Configure(o, config, environment.ContentRootPath));
        services.AddScoped<StoreScope>();
        services.AddScoped<StorefrontContent>();
        services.AddSingleton<Shop.Api.Operations.OperationJournal>();
        services.AddSingleton<Shop.Api.Operations.StoreBackups>();
        services.AddHostedService<Shop.Api.Operations.BackupScheduler>();
        services.AddScoped<OrderTrackingAccess>();
        services.AddScoped<Shop.Application.Accounts.IAccountLinkSettings, Shop.Api.Adapters.AccountLinkSettings>();
        services.AddScoped<Shop.Application.Accounts.ICustomerRecoveryStore, Shop.Infrastructure.Accounts.CustomerRecoveryAdapter>();
        services.AddScoped<Shop.Application.Accounts.CustomerRecoveryService>();
        services.AddScoped<Shop.Api.Security.CustomerEmailSecurity>();
        services.AddScoped<Shop.Application.Returns.IReturnStore, Shop.Infrastructure.Returns.SqlReturnStore>();
        services.AddScoped<Shop.Application.Returns.ReturnService>();
        services.AddScoped<Shop.Api.Security.OrderReturnsService>();
        services.AddScoped<Shop.Application.Notifications.IEmailPayloadProtector>(sp =>
        {
            var protector = sp.GetRequiredService<IDataProtectionProvider>().CreateProtector("StoreEmailOutbox", sp.GetRequiredService<StoreScope>().StoreId);
            return new Shop.Infrastructure.Notifications.EmailPayloadProtector(protector.Protect, protector.Unprotect);
        });
        services.AddScoped<Shop.Application.Notifications.IOutboxFailureJournal, Shop.Api.Adapters.OutboxFailureJournal>();
        services.AddScoped<Shop.Application.Notifications.IEmailQueue, Shop.Infrastructure.Notifications.SqlEmailQueue>();
        services.AddScoped<Shop.Api.Security.EmailOutbox>();
        services.AddScoped<IEmailOutbox>(sp => sp.GetRequiredService<Shop.Api.Security.EmailOutbox>());
        services.AddHostedService<Shop.Api.Security.EmailOutboxWorker>();
        services.AddDataProtection().SetApplicationName("ShopEmailOutbox").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(environment.ContentRootPath, "store-data", "email-keys")));
        services.AddScoped<IStoreIdentity>(sp => sp.GetRequiredService<StoreScope>());
        services.AddDbContext<ShopContext>((sp, o) => StoreConnections.Configure(o, config,
            sp.GetRequiredService<StoreScope>().Store?.ConnectionKey ?? (EF.IsDesignTime ? "DefaultStore" : throw new InvalidOperationException("Store resolution is required."))));

        // 2-3a. Repository/unit-of-work + cross-cutting services.
        services.AddScoped<IRepoWrapper, RepoWrapper>();
        services.AddScoped<Shop.Application.Orders.IOrderFulfillmentStore, Shop.Infrastructure.Orders.EfOrderFulfillmentStore>();
        services.AddScoped<Shop.Application.Orders.OrderFulfillmentService>();
        services.AddScoped<Shop.Application.Checkout.ICheckoutStore, Shop.Infrastructure.Checkout.EfCheckoutStore>();
        services.AddScoped<Shop.Application.Checkout.ICheckoutBoundary, Shop.Api.Adapters.CheckoutBoundary>();
        services.AddScoped<Shop.Application.Checkout.CheckoutService>();
        services.AddScoped<Shop.Application.Catalog.IProductCatalogStore, Shop.Infrastructure.Catalog.EfProductCatalogStore>();
        services.AddScoped<Shop.Application.Catalog.ProductManagementService>();
        services.AddScoped<Shop.Application.Provisioning.IPlatformRegistrationStore, Shop.Infrastructure.Provisioning.PlatformRegistrationStore>();
        services.AddScoped<Shop.Application.Provisioning.StoreProvisioningService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();


        return services;
    }
}
