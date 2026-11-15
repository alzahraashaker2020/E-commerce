using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Shop.DAL.Data;

/// <summary>Consistent schema-only snapshots and verified imports. No other schema's rows are exported.</summary>
public static class SqlServerDataArchive
{
    public sealed record Table(string Name, string[] Columns, List<string?[]> Rows, long? Identity, string Sha256);
    public sealed record Archive(int Version, string Schema, List<Table> Tables);
    private static string Q(string name) => SqlServerTenantScope.Q(name);
    private static string Normalize(object value) => value switch
    {
        decimal d => d.ToString("G29",CultureInfo.InvariantCulture),
        DateTime d => d.ToString("yyyy-MM-ddTHH:mm:ss.fffffff",CultureInfo.InvariantCulture),
        byte[] b => Convert.ToBase64String(b),
        _ => Convert.ToString(value,CultureInfo.InvariantCulture)!
    };
    private static string Hash(IEnumerable<string?[]> rows) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("\n",rows.Select(r=>JsonSerializer.Serialize(r)).OrderBy(r=>r,StringComparer.Ordinal)))));
    private static async Task<List<string>> Names(SqlConnection db,SqlTransaction tx,string schema)
    {
        using var cmd=new SqlCommand("SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID(@schema) ORDER BY name",db,tx);
        cmd.Parameters.AddWithValue("@schema",schema);using var reader=await cmd.ExecuteReaderAsync();var names=new List<string>();
        while(await reader.ReadAsync()) names.Add(reader.GetString(0));return names;
    }
    public static async Task<Archive> ExportAsync(string connection,string schema,CancellationToken ct=default)
    {
        if(schema!="dbo")SqlServerTenantScope.Validate(schema);
        await using var db=new SqlConnection(SqlServerTenantScope.AdminConnection(connection));await db.OpenAsync(ct);
        using var tx=db.BeginTransaction(IsolationLevel.Serializable);var tables=new List<Table>();
        foreach(var name in await Names(db,tx,schema))
        {
            using var cmd=new SqlCommand($"SELECT * FROM {Q(schema)}.{Q(name)}",db,tx){CommandTimeout=180};
            var rows=new List<string?[]>();string[] columns;
            using(var reader=await cmd.ExecuteReaderAsync(ct))
            {
                columns=Enumerable.Range(0,reader.FieldCount).Select(reader.GetName).ToArray();
                while(await reader.ReadAsync(ct)) rows.Add(Enumerable.Range(0,reader.FieldCount).Select(i=>reader.IsDBNull(i)?null:Normalize(reader.GetValue(i))).ToArray());
            }
            using var id=new SqlCommand("SELECT CONVERT(bigint,last_value) FROM sys.identity_columns WHERE object_id=OBJECT_ID(@table)",db,tx);
            id.Parameters.AddWithValue("@table",schema+"."+name);var last=await id.ExecuteScalarAsync(ct);
            tables.Add(new(name,columns,rows,last is null or DBNull?null:Convert.ToInt64(last),Hash(rows)));
        }
        tx.Commit();return new(1,schema,tables);
    }
    public static async Task ImportAsync(string connection,string schema,Archive archive,CancellationToken ct=default)
    {
        if(archive.Version!=1)throw new InvalidOperationException("Unsupported schema archive version.");
        if(schema!="dbo")SqlServerTenantScope.Validate(schema);
        await using var db=new SqlConnection(SqlServerTenantScope.AdminConnection(connection));await db.OpenAsync(ct);
        using var tx=db.BeginTransaction();
        async Task Exec(string sql){using var cmd=new SqlCommand(sql,db,tx){CommandTimeout=180};await cmd.ExecuteNonQueryAsync(ct);}
        var names=await Names(db,tx,schema);
        foreach(var name in names)await Exec($"ALTER TABLE {Q(schema)}.{Q(name)} NOCHECK CONSTRAINT ALL");
        foreach(var name in names)await Exec($"DELETE FROM {Q(schema)}.{Q(name)}");
        foreach(var table in archive.Tables)
        {
            if(Hash(table.Rows)!=table.Sha256)throw new InvalidOperationException("Schema archive hash mismatch.");
            if(!names.Contains(table.Name))
            {
                if(table.Name!="__ShopSchemaVersions")throw new InvalidOperationException("Unmapped archive table: "+table.Name);
                await Exec($"CREATE TABLE {Q(schema)}.__ShopSchemaVersions (Version int PRIMARY KEY,AppliedAtUtc nvarchar(40) NOT NULL)");
            }
            using var data=new DataTable();
            using(var cmd=new SqlCommand($"SELECT TOP(0) * FROM {Q(schema)}.{Q(table.Name)}",db,tx))
            using(var reader=await cmd.ExecuteReaderAsync(ct))
                for(int i=0;i<reader.FieldCount;i++)data.Columns.Add(reader.GetName(i),reader.GetFieldType(i));
            if(data.Columns.Count!=table.Columns.Length||table.Columns.Any(c=>!data.Columns.Contains(c)))throw new InvalidOperationException("Archive columns do not match target model.");
            foreach(var row in table.Rows)
            {
                var target=data.NewRow();
                for(int i=0;i<table.Columns.Length;i++)
                {
                    var value=row[i];var type=data.Columns[table.Columns[i]]!.DataType;
                    target[table.Columns[i]]=value is null?DBNull.Value:type==typeof(DateTime)?DateTime.Parse(value,CultureInfo.InvariantCulture):type==typeof(byte[])?Convert.FromBase64String(value):type==typeof(Guid)?Guid.Parse(value):Convert.ChangeType(value,type,CultureInfo.InvariantCulture);
                }
                data.Rows.Add(target);
            }
            // Parameterized inserts avoid SqlBulkCopy's auxiliary metadata session blocking
            // behind transactional schema locks during an isolated restore.
            if(table.Identity.HasValue)await Exec($"SET IDENTITY_INSERT {Q(schema)}.{Q(table.Name)} ON");
            foreach(DataRow row in data.Rows)
            {
                string fields=string.Join(",",data.Columns.Cast<DataColumn>().Select(c=>Q(c.ColumnName)));
                string values=string.Join(",",Enumerable.Range(0,data.Columns.Count).Select(i=>"@v"+i));
                using var insert=new SqlCommand($"INSERT INTO {Q(schema)}.{Q(table.Name)} ({fields}) VALUES ({values})",db,tx){CommandTimeout=180};
                for(int i=0;i<data.Columns.Count;i++){var parameter=insert.Parameters.AddWithValue("@v"+i,row[i]);if(data.Columns[i].DataType==typeof(DateTime)){parameter.SqlDbType=SqlDbType.DateTime2;parameter.Scale=7;}}
                await insert.ExecuteNonQueryAsync(ct);
            }
            if(table.Identity.HasValue)await Exec($"SET IDENTITY_INSERT {Q(schema)}.{Q(table.Name)} OFF");
            if(table.Identity.HasValue)await Exec($"DBCC CHECKIDENT ('{schema}.{table.Name.Replace("'","''")}',RESEED,{table.Identity.Value.ToString(CultureInfo.InvariantCulture)}) WITH NO_INFOMSGS");
            using var verify=new SqlCommand($"SELECT {string.Join(",",table.Columns.Select(Q))} FROM {Q(schema)}.{Q(table.Name)}",db,tx);
            using var r=await verify.ExecuteReaderAsync(ct);var actual=new List<string?[]>();while(await r.ReadAsync(ct))actual.Add(Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:Normalize(r.GetValue(i))).ToArray());
            if(actual.Count!=table.Rows.Count||Hash(actual)!=table.Sha256)throw new InvalidOperationException("Imported rows failed verification: "+table.Name);
        }
        foreach(var name in names)await Exec($"ALTER TABLE {Q(schema)}.{Q(name)} WITH CHECK CHECK CONSTRAINT ALL");
        tx.Commit();
    }
}
