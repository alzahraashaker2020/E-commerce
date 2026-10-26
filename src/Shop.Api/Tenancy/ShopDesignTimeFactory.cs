using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shop.DAL.Data;

namespace Shop.Api.Tenancy;

/// <summary>EF tooling uses an explicit configured reference, independently of HTTP store resolution.</summary>
public sealed class ShopDesignTimeFactory : IDesignTimeDbContextFactory<ShopContext>
{
    public ShopContext CreateDbContext(string[] args)
    {
        string root = File.Exists("appsettings.json") ? Directory.GetCurrentDirectory() : Path.Combine(Directory.GetCurrentDirectory(), "src", "Shop.Api");
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true).AddEnvironmentVariables().Build();
        string key = "DefaultStore";
        int argument = Array.IndexOf(args, "--store-connection");
        if (argument >= 0 && argument + 1 < args.Length) key = args[argument + 1];
        var options = new DbContextOptionsBuilder<ShopContext>();
        StoreConnections.Configure(options, config, key);
        return new ShopContext(options.Options);
    }
}

public sealed class PlatformDesignTimeFactory : IDesignTimeDbContextFactory<PlatformContext>
{
    public PlatformContext CreateDbContext(string[] args)
    {
        string root = File.Exists("appsettings.json") ? Directory.GetCurrentDirectory() : Path.Combine(Directory.GetCurrentDirectory(), "src", "Shop.Api");
        var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        return new PlatformContext(new DbContextOptionsBuilder<PlatformContext>()
            .UseSqlite(config.GetConnectionString("PlatformConnection") ?? "Data Source=" + Path.Combine(root, "shop_platform.db")).Options);
    }
}
