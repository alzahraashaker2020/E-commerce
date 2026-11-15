using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Composition;
using Shop.Api.Tenancy;
using Shop.Application.Common;
using Shop.BLL.Email;
using Shop.DAL.Data;
using Shop.Infrastructure.Notifications;
using Xunit;
namespace Shop.RegressionTests;

public class HostCompositionTests
{
    [Fact]
    public async Task Request_scope_resolves_shared_context_and_async_composer()
    {
        var root = Path.Combine(Path.GetTempPath(), "shop-composition-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Database:Provider", "Sqlite" }, { "ConnectionStrings:SqliteConnection", "Data Source=" + Path.Combine(root, "store.db") }, { "ConnectionStrings:PlatformConnection", "Data Source=" + Path.Combine(root, "platform.db") } }).Build();
        var env = new EnvironmentStub { ContentRootPath = root }; var services = new ServiceCollection(); services.AddLogging(); services.AddHttpContextAccessor(); services.AddSingleton<IWebHostEnvironment>(env); services.AddSingleton<IHostEnvironment>(env); services.AddSingleton<IConfiguration>(config); services.AddSingleton(new DeploymentProfile(config)); services.AddSingleton<IClock, Shop.Api.Adapters.SystemClock>();
        services.AddShopData(config, env); services.AddShopEmail(config, env);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }); await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<StoreScope>().Store = new() { Id = "default", ConnectionKey = "DefaultStore", Host = "test.localhost", Name = "Test" };
        var db = scope.ServiceProvider.GetRequiredService<ShopContext>(); await db.Database.EnsureCreatedAsync(); SqliteSchemaUpgrade.Apply(db, true);
        Assert.Same(db, scope.ServiceProvider.GetRequiredService<ShopContext>()); Assert.IsType<StoreEmailComposer>(scope.ServiceProvider.GetRequiredService<IEmailService>());
    }
    [Fact]
    public void Application_has_no_HTTP_EF_or_persistence_project_references()
    {
        var references = typeof(Shop.Application.Checkout.CheckoutService).Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();
        Assert.DoesNotContain(references, n => n.StartsWith("Shop.") || n.StartsWith("Microsoft.AspNetCore") || n.Contains("EntityFramework") || n.Contains("Npgsql"));
        Assert.DoesNotContain(typeof(SqlEmailQueue).Assembly.GetReferencedAssemblies(), a => a.Name == "Shop.Api");
    }
    private sealed class EnvironmentStub : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development"; public string ApplicationName { get; set; } = "Shop.Api"; public string WebRootPath { get; set; } = ""; public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public string ContentRootPath { get; set; } = ""; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
