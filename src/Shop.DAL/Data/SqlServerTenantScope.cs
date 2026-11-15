using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shop.DAL.Data;

/// <summary>One database with separate schemas and database principals for store data.</summary>
public static class SqlServerTenantScope
{
    private const string Prefix = "MatjarkSchema/";
    public static string? Schema(string connection)
    {
        string app = new SqlConnectionStringBuilder(connection).ApplicationName;
        return app.StartsWith(Prefix, StringComparison.Ordinal) ? Validate(app[Prefix.Length..]) : null;
    }
    public static string Validate(string schema)
    {
        if (!Regex.IsMatch(schema, "^tenant_[a-z0-9_]{1,70}$")) throw new InvalidOperationException("Use a dedicated tenant schema name.");
        return schema;
    }
    public static string Connection(string connection, string schema) => new SqlConnectionStringBuilder(connection)
        { ApplicationName = Prefix + Validate(schema), Pooling = false }.ToString();
    public static string AdminConnection(string connection) => new SqlConnectionStringBuilder(connection)
        { ApplicationName = "MatjarkAdministration", Pooling = false }.ToString();
    public static string Principal(string schema) => "matjark_" + Validate(schema);
    public static string Q(string value) => "[" + value.Replace("]", "]]") + "]";
    public static async Task<bool> ExistsAsync(string connection)
    {
        string schema = Schema(connection) ?? throw new InvalidOperationException("Tenant schema required.");
        await using var db = new SqlConnection(AdminConnection(connection)); await db.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.schemas WHERE name=@schema", db);
        cmd.Parameters.AddWithValue("@schema", schema);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 0;
    }
    public static async Task CreateAsync(string connection)
    {
        string schema = Schema(connection) ?? throw new InvalidOperationException("Tenant schema required.");
        string user = Principal(schema);
        await using var admin = new SqlConnection(AdminConnection(connection)); await admin.OpenAsync();
        using (var cmd = new SqlCommand($"CREATE USER {Q(user)} WITHOUT LOGIN WITH DEFAULT_SCHEMA={Q(schema)}; EXEC(N'CREATE SCHEMA {Q(schema)} AUTHORIZATION {Q(user)}'); GRANT CREATE TABLE TO {Q(user)};", admin))
            await cmd.ExecuteNonQueryAsync(); // Existing schemas/users fail closed; never adopt them.
        try
        {
            var options = new DbContextOptionsBuilder<ShopContext>(); Configure(options, connection);
            await using var tenant = new ShopContext(options.Options);
            // Unqualified identifiers resolve through the impersonated principal's default schema.
            await tenant.Database.OpenConnectionAsync();
            foreach (string batch in Regex.Split(tenant.Database.GenerateCreateScript(), @"^GO\s*$", RegexOptions.Multiline))
                if (!string.IsNullOrWhiteSpace(batch)) await tenant.Database.ExecuteSqlRawAsync(batch);
            SqlServerSchema.Apply(tenant, true);
        }
        finally
        {
            using var revoke = new SqlCommand($"REVOKE CREATE TABLE FROM {Q(user)}", admin);
            await revoke.ExecuteNonQueryAsync();
        }
    }
    public static void Configure(DbContextOptionsBuilder options, string connection) => options.UseSqlServer(connection).AddInterceptors(new ScopeInterceptor());
    private sealed class ScopeInterceptor : DbConnectionInterceptor
    {
        private static string Sql(DbConnection db) => $"EXECUTE AS USER = N'{Principal(Schema(db.ConnectionString)!)}'";
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            if (Schema(connection.ConnectionString) is null) return;
            using var cmd = connection.CreateCommand(); cmd.CommandText = Sql(connection); cmd.ExecuteNonQuery();
        }
        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Schema(connection.ConnectionString) is null) return;
            using var cmd = connection.CreateCommand(); cmd.CommandText = Sql(connection); await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
