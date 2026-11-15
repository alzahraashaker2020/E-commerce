using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Api.Tenancy;
using Shop.Api.Operations;
using Shop.DAL.Data;

if (args.FirstOrDefault() == "--test-backup")
{
    using var fixture=JsonDocument.Parse(await File.ReadAllTextAsync(args[1]));
    string connection=fixture.RootElement.GetProperty("Default").GetString()!;
    if(!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("matjark_test_"))throw new InvalidOperationException("Test database required.");
    await SqlServerBackups.CreateAsync(connection,args[2],Path.Combine(Path.GetDirectoryName(args[2])!,"native-backup-test"),default);
    var result=await SqlServerBackups.VerifyAsync(connection,args[2],false,Path.Combine(Path.GetDirectoryName(args[2])!,"native-backup-test"),default);
    Console.WriteLine(JsonSerializer.Serialize(result));return;
}
// Isolated regression fixture; no live config or existing database is touched.
if (args.FirstOrDefault() == "--test-fixture")
{
    string id="matjark_test_"+Guid.NewGuid().ToString("N");
    string Conn(string suffix)=>new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=id+suffix,IntegratedSecurity=true,TrustServerCertificate=true,Pooling=false}.ToString();
    string connection=Conn("_default");
    using(var db=new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlServer(connection).Options))
    {bool created=await db.Database.EnsureCreatedAsync();SqlServerSchema.Apply(db,created);}
    await File.WriteAllTextAsync(args[1],JsonSerializer.Serialize(new{Default=connection,Platform=Conn("_platform"),Alpha=Conn("_alpha"),Beta=Conn("_beta")}));
    return;
}
// Explicit offline transfer: only creates NEW target databases, never overwrites a database.
if (args.Length < 2) throw new ArgumentException("Usage: Shop.DatabaseTransfer <API content root> <new database prefix> [server=localhost]");
string root = Path.GetFullPath(args[0]), prefix = args[1], server = args.ElementAtOrDefault(2) ?? "localhost";
if (!System.Text.RegularExpressions.Regex.IsMatch(prefix, "^[A-Za-z][A-Za-z0-9_]{0,60}$")) throw new ArgumentException("Invalid new database prefix.");
using var instanceLock = OfflineRecovery.AcquireInstanceLock(root);
var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json",true)
    .AddJsonFile("email-secrets.json",true).Build();
if (PlatformDatabase.IsSqlServer(config)) throw new InvalidOperationException("Source is already SQL Server; this utility imports SQLite only.");
string evidence = Path.Combine(root,"obj","sqlserver-transfer",prefix);
if (Directory.Exists(evidence)) throw new InvalidOperationException("Use a new prefix; an earlier transfer exists.");
Directory.CreateDirectory(evidence);
string Connection(string name) => new SqlConnectionStringBuilder { DataSource=server,InitialCatalog=name,IntegratedSecurity=true,TrustServerCertificate=true,Pooling=false }.ToString();
string Q(string name) => "["+name.Replace("]","]]")+"]";
async Task Execute(SqlConnection db,string sql,SqlTransaction? tx=null) { using var cmd=new SqlCommand(sql,db,tx){CommandTimeout=180};await cmd.ExecuteNonQueryAsync(); }
var transfers = new List<object>();
var targets = new Dictionary<string,string>();
async Task<string> Snapshot(string connection,string name)
{
    var b=new SqliteConnectionStringBuilder(connection){Mode=SqliteOpenMode.ReadOnly,Pooling=false};
    if (!Path.IsPathRooted(b.DataSource)) b.DataSource=Path.GetFullPath(b.DataSource,root);
    await using var source=new SqliteConnection(b.ToString());await source.OpenAsync();
    string path=Path.Combine(evidence,name+".sqlite");
    await using var copy=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString());await copy.OpenAsync();source.BackupDatabase(copy);
    return path;
}
string platformSnapshot=await Snapshot(PlatformDatabase.Connection(config,root),"platform");
using var sourcePlatform = new PlatformContext(new DbContextOptionsBuilder<PlatformContext>().UseSqlite("Data Source="+platformSnapshot).Options);
var stores=await sourcePlatform.Stores.AsNoTracking().OrderBy(s=>s.Id).ToListAsync();
var sources=new Dictionary<string,string>();
foreach(var key in stores.Select(s=>s.ConnectionKey).Append("DefaultStore").Distinct())
{
    var source=StoreConnections.Read(config,key);
    if(source.Provider!="Sqlite")throw new InvalidOperationException("This transfer requires SQLite sources for every store.");
    sources[key]=await Snapshot(source.Connection,"store_"+sources.Count);
    targets[key]=Connection(prefix+"_store_"+(sources.Count-1));
}
async Task Import(string snapshot,string connection,bool platform)
{
    var builder=new SqlConnectionStringBuilder(connection);
    await using(var master=new SqlConnection(Connection("master")))
    {
        await master.OpenAsync();
        using var check=new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name=@name",master);check.Parameters.AddWithValue("@name",builder.InitialCatalog);
        if(Convert.ToInt32(await check.ExecuteScalarAsync())!=0)throw new InvalidOperationException("Target database already exists; refusing to overwrite it.");
        await Execute(master,"CREATE DATABASE "+Q(builder.InitialCatalog)+" COLLATE Latin1_General_100_BIN2");
    }
    if(platform)
    {
        using var db=new PlatformContext(new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(connection).Options);
        bool created=await db.Database.EnsureCreatedAsync();PlatformDatabase.Initialize(db,created);
        await db.Database.ExecuteSqlRawAsync(OperationJournal.SqlServerJournalSchema);
    }
    else
    {
        using var db=new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlServer(connection).Options);
        bool created=await db.Database.EnsureCreatedAsync();SqlServerSchema.Apply(db,created);
    }
    await using var sqlite=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=snapshot,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());await sqlite.OpenAsync();
    var tables=new List<string>();
    using(var cmd=sqlite.CreateCommand()){cmd.CommandText="SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())tables.Add(r.GetString(0));}
    await using var sql=new SqlConnection(connection);await sql.OpenAsync();using var tx=sql.BeginTransaction();
    var results=new List<object>();
    try
    {
        // Seed rows are removed only inside this newly created, unpublished target database.
        using(var cmd=new SqlCommand("SELECT name FROM sys.tables",sql,tx))
        {using var r=await cmd.ExecuteReaderAsync();var names=new List<string>();while(await r.ReadAsync())names.Add(r.GetString(0));r.Close();foreach(var t in names)await Execute(sql,$"ALTER TABLE {Q(t)} NOCHECK CONSTRAINT ALL",tx);foreach(var t in names.Where(t=>t!="__SqlServerSchema"))await Execute(sql,$"DELETE FROM {Q(t)}",tx);}
        foreach(string table in tables)
        {
            using var exists=new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name=@name",sql,tx);exists.Parameters.AddWithValue("@name",table);
            if(Convert.ToInt32(await exists.ExecuteScalarAsync())==0)
            {
                // Preserve source-only metadata tables too; do not silently omit unknown business tables.
                if(table!="__ShopSchemaVersions")throw new InvalidOperationException("Unmapped source table: "+table);
                await Execute(sql,"CREATE TABLE __ShopSchemaVersions (Version int PRIMARY KEY,AppliedAtUtc nvarchar(40) NOT NULL)",tx);
            }
            var data=new DataTable(table);
            using(var schema=new SqlCommand("SELECT TOP(0) * FROM "+Q(table),sql,tx))
            {using var r=await schema.ExecuteReaderAsync();for(int i=0;i<r.FieldCount;i++)data.Columns.Add(r.GetName(i),r.GetFieldType(i));}
            using(var cmd=sqlite.CreateCommand())
            {
                cmd.CommandText="SELECT * FROM "+Q(table);using var r=await cmd.ExecuteReaderAsync();
                var fields=Enumerable.Range(0,r.FieldCount).Select(r.GetName).ToArray();
                if(fields.Length!=data.Columns.Count || fields.Any(f=>!data.Columns.Contains(f)))throw new InvalidOperationException("Column mismatch: "+table);
                while(await r.ReadAsync())
                {
                    var row=data.NewRow();
                    foreach(string field in fields)
                    {var value=r[field];Type type=data.Columns[field]!.DataType;row[field]=value is DBNull?DBNull.Value: type==typeof(DateTime)?DateTime.Parse(Convert.ToString(value,CultureInfo.InvariantCulture)!,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind):type==typeof(bool)?Convert.ToInt64(value)!=0:Convert.ChangeType(value,type,CultureInfo.InvariantCulture);}
                    data.Rows.Add(row);
                }
            }
            using(var bulk=new SqlBulkCopy(sql,SqlBulkCopyOptions.KeepIdentity,tx){DestinationTableName=Q(table),BulkCopyTimeout=180})
            {foreach(DataColumn col in data.Columns)bulk.ColumnMappings.Add(col.ColumnName,col.ColumnName);await bulk.WriteToServerAsync(data);}
            var actual=new DataTable();using(var cmd=new SqlCommand("SELECT * FROM "+Q(table),sql,tx))using(var r=await cmd.ExecuteReaderAsync())actual.Load(r);
            string Hash(DataTable rows)
            {
                var values=rows.Rows.Cast<DataRow>().Select(row=>JsonSerializer.Serialize(rows.Columns.Cast<DataColumn>().Select(c=>row[c] is DBNull?null:row[c] is DateTime d?d.ToString("yyyy-MM-ddTHH:mm:ss.fffffff",CultureInfo.InvariantCulture):row[c] is decimal money?money.ToString("G29",CultureInfo.InvariantCulture):Convert.ToString(row[c],CultureInfo.InvariantCulture)).ToArray())).OrderBy(x=>x,StringComparer.Ordinal);
                return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",values))));
            }
            string hash=Hash(data);if(data.Rows.Count!=actual.Rows.Count||hash!=Hash(actual))throw new InvalidOperationException("Data verification failed: "+table);
            results.Add(new{table,rows=data.Rows.Count,sha256=hash});
        }
        // Preserve SQLite AUTOINCREMENT high-water marks, including deleted historical IDs.
        using(var sequence=sqlite.CreateCommand())
        {
            sequence.CommandText="SELECT name,seq FROM sqlite_sequence";
            using var rows=await sequence.ExecuteReaderAsync();
            while(await rows.ReadAsync())
            {
                string table=rows.GetString(0);long last=rows.GetInt64(1);
                using var identity=new SqlCommand("SELECT COUNT(*) FROM sys.identity_columns WHERE object_id=OBJECT_ID(@table)",sql,tx);identity.Parameters.AddWithValue("@table",table);
                if(Convert.ToInt32(await identity.ExecuteScalarAsync())!=0)
                    await Execute(sql,$"DBCC CHECKIDENT ({Q(table)}, RESEED, {last.ToString(CultureInfo.InvariantCulture)}) WITH NO_INFOMSGS",tx);
            }
        }
        using(var cmd=new SqlCommand("SELECT name FROM sys.tables",sql,tx))
        {using var r=await cmd.ExecuteReaderAsync();var names=new List<string>();while(await r.ReadAsync())names.Add(r.GetString(0));r.Close();foreach(var t in names)await Execute(sql,$"ALTER TABLE {Q(t)} WITH CHECK CHECK CONSTRAINT ALL",tx);}
        await tx.CommitAsync();
    }
    catch { await tx.RollbackAsync();throw; }
    transfers.Add(new{database=builder.InitialCatalog,tables=results});Console.WriteLine("Verified "+builder.InitialCatalog);
}
foreach(var entry in sources)await Import(entry.Value,targets[entry.Key],false);
string platformTarget=Connection(prefix+"_platform");await Import(platformSnapshot,platformTarget,true);
// Database identity is the one intentional transformed value; IDs and hosts stay unchanged.
await using(var db=new PlatformContext(new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(platformTarget).Options))
{
    foreach(var store in await db.Stores.ToListAsync())
    {store.DatabaseIdentity=StoreConnections.SqlServerIdentity(targets[store.ConnectionKey]);}
    await db.SaveChangesAsync();
}
var overlay=new Dictionary<string,object>{["Database"]=new{Provider="SqlServer"},["Platform"]=new{DatabaseProvider="SqlServer"},
    ["ConnectionStrings"]=new{DefaultConnection=targets["DefaultStore"],PlatformConnection=platformTarget},
    ["StoreConnections"]=targets.Where(e=>e.Key!="DefaultStore").ToDictionary(e=>e.Key,e=>(object)new{Provider="SqlServer",ConnectionString=e.Value})};
var json=new JsonSerializerOptions{WriteIndented=true};
await File.WriteAllTextAsync(Path.Combine(evidence,"sqlserver-local.json"),JsonSerializer.Serialize(overlay,json));
var assets=new List<object>();
foreach(string folder in new[]{"wwwroot/uploads","store-data/email-keys"})
if(Directory.Exists(Path.Combine(root,folder)))foreach(string file in Directory.EnumerateFiles(Path.Combine(root,folder),"*",SearchOption.AllDirectories))
{using var input=File.OpenRead(file);assets.Add(new{path=Path.GetRelativePath(root,file),sha256=Convert.ToHexString(SHA256.HashData(input))});}
await File.WriteAllTextAsync(Path.Combine(evidence,"verification.json"),JsonSerializer.Serialize(new{createdUtc=DateTime.UtcNow,transfers,assets},json));
Console.WriteLine("Transfer verified. Configuration prepared, NOT activated: "+evidence);

