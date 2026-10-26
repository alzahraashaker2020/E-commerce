using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Shop.DAL.Data;

namespace Shop.Api.Tenancy;

public static class StoreConnections
{
    public static (string Provider, string Connection) Read(IConfiguration config, string key)
    {
        if (key == "DefaultStore")
            return (config["Database:Provider"] ?? "Sqlite",
                config.GetConnectionString(string.Equals(config["Database:Provider"], "Postgres", StringComparison.OrdinalIgnoreCase)
                    ? "DefaultConnection" : "SqliteConnection") ?? "Data Source=shop_demo.db");
        var section = config.GetSection("StoreConnections:" + key);
        var provider = section["Provider"];
        var connection = section["ConnectionString"];
        if (string.IsNullOrEmpty(connection) || provider is not ("Sqlite" or "Postgres"))
            throw new InvalidOperationException("Store connection reference is not configured.");
        return (provider, connection);
    }

    public static void Configure(DbContextOptionsBuilder options, IConfiguration config, string key)
    {
        var (provider, connection) = Read(config, key);
        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)) options.UseNpgsql(connection);
        else options.UseSqlite(connection);
    }

    public static string DatabaseIdentity(IConfiguration config, string key)
    {
        var (provider, connection) = Read(config, key);
        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var sqlite = new SqliteConnectionStringBuilder(connection);
            if (sqlite.DataSource == ":memory:" || sqlite.Mode == SqliteOpenMode.Memory)
                throw new InvalidOperationException("A persistent database is required.");
            return "sqlite:" + Path.GetFullPath(sqlite.DataSource).ToLowerInvariant();
        }
        var pg = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        return $"postgres:{(pg.Host ?? "localhost").ToLowerInvariant()}:{pg.Port}:{pg.Database}";
    }
}
