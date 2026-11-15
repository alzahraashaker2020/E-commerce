using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Shop.Api.Tenancy;

public static class PlatformDatabase
{
    public static bool IsSqlServer(IConfiguration config) => string.Equals(config["Platform:DatabaseProvider"], "SqlServer", StringComparison.OrdinalIgnoreCase);
    public static string Connection(IConfiguration config, string root) => config.GetConnectionString("PlatformConnection")
        ?? new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = Path.Combine(root,"shop_platform.db") }.ToString();
    public static void Configure(DbContextOptionsBuilder options, IConfiguration config, string root)
    {
        if (IsSqlServer(config)) options.UseSqlServer(Connection(config, root));
        else options.UseSqlite(Connection(config, root));
    }
    public static void Initialize(PlatformContext db, bool created)
    {
        if (!created) return;
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE PlatformWebsite (Id int PRIMARY KEY,Version int NOT NULL,Document nvarchar(max) NOT NULL);
            CREATE TABLE MerchantOnboardingSetup (Id int PRIMARY KEY);
            """);
    }
}
