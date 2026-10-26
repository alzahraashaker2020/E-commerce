using Microsoft.Data.Sqlite;

namespace Shop.Api.Operations;

/// <summary>Platform-side metadata only; never stores request bodies, queries or credentials.</summary>
public sealed class OperationJournal
{
    private readonly string _connection;
    private readonly ILogger<OperationJournal> _logger;
    public OperationJournal(IConfiguration config,IWebHostEnvironment environment,ILogger<OperationJournal> logger)
    {_connection=config.GetConnectionString("PlatformConnection")??"Data Source="+Path.Combine(environment.ContentRootPath,"shop_platform.db");_logger=logger;}
    private async Task<SqliteConnection> Open(){var connection=new SqliteConnection(_connection);await connection.OpenAsync();return connection;}
    public async Task InitializeAsync()
    {
        await using var db=await Open();using var command=db.CreateCommand();command.CommandText="""
        CREATE TABLE IF NOT EXISTS OperationEvents (
          Id TEXT PRIMARY KEY, StoreId TEXT NOT NULL, Actor TEXT NOT NULL, Role TEXT NOT NULL,
          Action TEXT NOT NULL, Method TEXT NOT NULL, Status INTEGER NOT NULL, TraceId TEXT NOT NULL,
          FailureType TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, AcknowledgedAtUtc TEXT NOT NULL DEFAULT '', AcknowledgedBy TEXT NOT NULL DEFAULT '');
        CREATE INDEX IF NOT EXISTS IX_OperationEvents_StoreTime ON OperationEvents(StoreId,CreatedAtUtc);
        CREATE TABLE IF NOT EXISTS BackupPolicy (Id INTEGER PRIMARY KEY, Enabled INTEGER NOT NULL, IntervalHours INTEGER NOT NULL);
        INSERT OR IGNORE INTO BackupPolicy VALUES (1,0,24);
        CREATE TABLE IF NOT EXISTS StoreSecurityVersions (StoreId TEXT PRIMARY KEY, Version TEXT NOT NULL);
        """;await command.ExecuteNonQueryAsync();
    }
    public async Task WriteAsync(string store,string actor,string role,string action,string method,int status,string trace,string failure="")
    {
        try
        {
            await using var db=await Open();using var command=db.CreateCommand();
            command.CommandText="INSERT INTO OperationEvents (Id,StoreId,Actor,Role,Action,Method,Status,TraceId,FailureType,CreatedAtUtc) VALUES ($id,$store,$actor,$role,$action,$method,$status,$trace,$failure,$at)";
            foreach(var value in new Dictionary<string,object>{{"id",Guid.NewGuid().ToString("N")},{"store",store},{"actor",actor},{"role",role},{"action",action},{"method",method},{"status",status},{"trace",trace},{"failure",failure},{"at",DateTime.UtcNow.ToString("O")}})command.Parameters.AddWithValue("$"+value.Key,value.Value);
            await command.ExecuteNonQueryAsync();
        }
        catch(Exception){_logger.LogWarning("Operation journal is temporarily unavailable.");}
    }
    public async Task<List<Event>> ReadAsync(string? store=null,bool errorsOnly=false)
    {
        await using var db=await Open();using var command=db.CreateCommand();
        command.CommandText="SELECT Id,StoreId,Actor,Role,Action,Method,Status,TraceId,FailureType,CreatedAtUtc,AcknowledgedAtUtc,AcknowledgedBy FROM OperationEvents WHERE ($store IS NULL OR StoreId=$store) AND ($errors=0 OR Status>=500) ORDER BY CreatedAtUtc DESC LIMIT 100";
        command.Parameters.AddWithValue("$store",(object?)store??DBNull.Value);command.Parameters.AddWithValue("$errors",errorsOnly?1:0);
        using var reader=await command.ExecuteReaderAsync();var rows=new List<Event>();
        while(await reader.ReadAsync())rows.Add(new(reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.GetString(3),reader.GetString(4),reader.GetString(5),reader.GetInt32(6),reader.GetString(7),reader.GetString(8),reader.GetString(9),reader.GetString(10),reader.GetString(11)));
        return rows;
    }
    public async Task<bool> AcknowledgeAsync(string id,string actor,string? store=null)
    {
        await using var db=await Open();using var command=db.CreateCommand();command.CommandText="UPDATE OperationEvents SET AcknowledgedAtUtc=$at,AcknowledgedBy=$actor WHERE Id=$id AND Status>=500 AND AcknowledgedAtUtc='' AND ($store IS NULL OR StoreId=$store)";
        command.Parameters.AddWithValue("$store",(object?)store??DBNull.Value);
        command.Parameters.AddWithValue("$at",DateTime.UtcNow.ToString("O"));command.Parameters.AddWithValue("$actor",actor);command.Parameters.AddWithValue("$id",id);return await command.ExecuteNonQueryAsync()==1;
    }
    public async Task<Policy> ReadPolicyAsync()
    {
        await using var db=await Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT Enabled,IntervalHours FROM BackupPolicy WHERE Id=1";using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();return new(r.GetInt32(0)==1,r.GetInt32(1));
    }
    public async Task SetPolicyAsync(Policy policy)
    {await using var db=await Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE BackupPolicy SET Enabled=$enabled,IntervalHours=$hours WHERE Id=1";cmd.Parameters.AddWithValue("$enabled",policy.Enabled?1:0);cmd.Parameters.AddWithValue("$hours",policy.IntervalHours);await cmd.ExecuteNonQueryAsync();}
    public async Task<string> ReadSecurityVersionAsync(string store)
    {await using var db=await Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT Version FROM StoreSecurityVersions WHERE StoreId=$store";cmd.Parameters.AddWithValue("$store",store);return Convert.ToString(await cmd.ExecuteScalarAsync())??"";}
    public async Task RotateSecurityVersionAsync(string store)
    {await using var db=await Open();using var cmd=db.CreateCommand();cmd.CommandText="INSERT INTO StoreSecurityVersions VALUES ($store,$version) ON CONFLICT(StoreId) DO UPDATE SET Version=excluded.Version";cmd.Parameters.AddWithValue("$store",store);cmd.Parameters.AddWithValue("$version",Guid.NewGuid().ToString("N"));await cmd.ExecuteNonQueryAsync();}
    public sealed record Policy(bool Enabled,int IntervalHours);
    public sealed record Event(string Id,string StoreId,string Actor,string Role,string Action,string Method,int Status,string TraceId,string FailureType,string CreatedAtUtc,string AcknowledgedAtUtc,string AcknowledgedBy);
}
