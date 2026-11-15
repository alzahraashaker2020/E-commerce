using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Shop.DAL.Data;

namespace Shop.Api.Tenancy;

public static class StoreConnections
{
    public static (string Provider, string Connection) Read(IConfiguration config, string key)
    {
        if (key == "DefaultStore")
        {
            string defaultProvider = config["Database:Provider"] ?? "Sqlite";
            string defaultConnection = config.GetConnectionString(defaultProvider is "Postgres" or "SqlServer" ? "DefaultConnection" : "SqliteConnection") ?? "Data Source=shop_demo.db";
            if (defaultProvider == "SqlServer" && config["Database:Schema"] is string defaultSchema && defaultSchema != "") defaultConnection=SqlServerTenantScope.Connection(defaultConnection,defaultSchema);
            CheckSharedLayout(config,defaultProvider,defaultConnection);
            return (defaultProvider,defaultConnection);
        }
        var section = config.GetSection("StoreConnections:" + key);
        var provider = section["Provider"];
        var connection = section["ConnectionString"];
        if (string.IsNullOrEmpty(connection) || provider is not ("Sqlite" or "Postgres" or "SqlServer"))
            throw new InvalidOperationException("Store connection reference is not configured.");
        if(provider=="SqlServer" && section["Schema"] is string schema && schema!="") connection=SqlServerTenantScope.Connection(connection,schema);
        CheckSharedLayout(config,provider,connection);
        return (provider, connection);
    }

    private static void CheckSharedLayout(IConfiguration config,string provider,string connection)
    {
        if(config["Database:Layout"]!="SharedSchemas") return;
        if(provider!="SqlServer" || SqlServerTenantScope.Schema(connection) is null) throw new InvalidOperationException("Shared-database stores require a dedicated SQL Server schema.");
        var target=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        var shared=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(config.GetConnectionString("PlatformConnection"));
        if(!string.Equals(target.DataSource,shared.DataSource,StringComparison.OrdinalIgnoreCase)||!string.Equals(target.InitialCatalog,shared.InitialCatalog,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("All stores in shared mode must use the configured platform database.");
    }
    public static void Configure(DbContextOptionsBuilder options, IConfiguration config, string key)
    {
        var (provider, connection) = Read(config, key);
        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)) options.UseNpgsql(connection);
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)) SqlServerTenantScope.Configure(options, connection);
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
        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var sql = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
            if (string.IsNullOrWhiteSpace(sql.InitialCatalog)) throw new InvalidOperationException("A separate named database is required.");
            return SqlServerIdentity(connection);
        }
        var pg = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        return $"postgres:{(pg.Host ?? "localhost").ToLowerInvariant()}:{pg.Port}:{pg.Database}";
    }

    public static string SqlServerIdentity(string connection)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        string database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) throw new InvalidOperationException("A separate named database is required.");
        builder.InitialCatalog = "master";
        using var db = new Microsoft.Data.SqlClient.SqlConnection(builder.ToString());
        db.Open();
        using var command = db.CreateCommand();
        command.CommandText = "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))";
        string server = Convert.ToString(command.ExecuteScalar()) ?? throw new InvalidOperationException("SQL Server identity unavailable.");
        // localhost, '.', machine-name and client aliases cannot register one database twice.
        string? schema=SqlServerTenantScope.Schema(connection);
        return $"sqlserver:{server.ToLowerInvariant()}:{database.ToLowerInvariant()}"+(schema is null?"":":schema:"+schema);
    }
}
