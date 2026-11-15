using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Api.Tenancy;
using Shop.Api.Operations;
using Shop.DAL.Data;

if(args.Length==3 && args[0]=="--test-schema-backup")
{
    using var doc=JsonDocument.Parse(await File.ReadAllTextAsync(args[1]));
    string conn=doc.RootElement.GetProperty("Default").GetString()!;
    if(!new SqlConnectionStringBuilder(conn).InitialCatalog.StartsWith("matjark_test_"))throw new InvalidOperationException("Test fixture required.");
    Console.WriteLine("Exporting schema snapshot");await SqlServerSchemaBackups.CreateAsync(conn,args[2],default);
    Console.WriteLine("Restoring snapshot to separate database");var result=await SqlServerSchemaBackups.VerifyAsync(conn,args[2],default);
    Console.WriteLine($"Verified tables={result.Tables}, products={result.Products}, orders={result.Orders}");return;
}
if(args.Length==2 && args[0]=="--test-fixture")
{
    string fixtureName="matjark_test_"+Guid.NewGuid().ToString("N");
    string shared=new SqlConnectionStringBuilder{DataSource=@".\MAXSYSTEM",InitialCatalog=fixtureName,IntegratedSecurity=true,TrustServerCertificate=true,Pooling=false}.ToString();
    using(var db=new PlatformContext(new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(shared).Options))
    {bool created=await db.Database.EnsureCreatedAsync();PlatformDatabase.Initialize(db,created);await db.Database.ExecuteSqlRawAsync(OperationJournal.SqlServerJournalSchema);}
    string Scope(string schema)=>SqlServerTenantScope.Connection(shared,schema);
    await SqlServerTenantScope.CreateAsync(Scope("tenant_default"));
    await File.WriteAllTextAsync(args[1],JsonSerializer.Serialize(new{Default=Scope("tenant_default"),Platform=shared,Alpha=Scope("tenant_alpha"),Beta=Scope("tenant_beta")}));return;
}
if(args.Length!=3)throw new ArgumentException("Usage: <API root> <SQL instance> <new database name>");
string root=Path.GetFullPath(args[0]),server=args[1],name=args[2];
if(!Regex.IsMatch(name,"^[A-Za-z][A-Za-z0-9_]{0,90}$"))throw new ArgumentException("Invalid database name.");
using var instanceLock=OfflineRecovery.AcquireInstanceLock(root);
var config=new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json",true)
    .AddJsonFile("sqlserver-local.json").AddJsonFile("email-secrets.json",true).Build();
string folder=Path.Combine(root,"obj","unified-transfer",name+"-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"));Directory.CreateDirectory(folder);
string sourcePlatform=PlatformDatabase.Connection(config,root);
using var platform=new PlatformContext(new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(sourcePlatform).Options);
var stores=await platform.Stores.AsNoTracking().OrderBy(s=>s.Id).ToListAsync();
var snapshots=new Dictionary<string,SqlServerDataArchive.Archive>();
var platformArchive=await SqlServerDataArchive.ExportAsync(sourcePlatform,"dbo");
await File.WriteAllTextAsync(Path.Combine(folder,"platform.json"),JsonSerializer.Serialize(platformArchive));
foreach(var key in stores.Select(s=>s.ConnectionKey).Append("DefaultStore").Distinct())
{
    var source=StoreConnections.Read(config,key);if(source.Provider!="SqlServer")throw new InvalidOperationException("This transfer requires SQL Server sources.");
    snapshots[key]=await SqlServerDataArchive.ExportAsync(source.Connection,SqlServerTenantScope.Schema(source.Connection)??"dbo");
    await File.WriteAllTextAsync(Path.Combine(folder,key+".json"),JsonSerializer.Serialize(snapshots[key]));
}
string connection=new SqlConnectionStringBuilder{DataSource=server,InitialCatalog=name,IntegratedSecurity=true,TrustServerCertificate=true,Pooling=false}.ToString();
var masterConnection=new SqlConnectionStringBuilder(connection){InitialCatalog="master"}.ToString();
await using(var master=new SqlConnection(masterConnection))
{
    await master.OpenAsync();using var check=new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name=@name",master);check.Parameters.AddWithValue("@name",name);
    if(Convert.ToInt32(await check.ExecuteScalarAsync())!=0)throw new InvalidOperationException("Destination already exists. Refusing to overwrite it.");
    using var create=new SqlCommand("CREATE DATABASE "+SqlServerTenantScope.Q(name)+" COLLATE Latin1_General_100_BIN2",master);await create.ExecuteNonQueryAsync();
}
using(var target=new PlatformContext(new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(connection).Options))
{bool created=await target.Database.EnsureCreatedAsync();PlatformDatabase.Initialize(target,created);await target.Database.ExecuteSqlRawAsync(OperationJournal.SqlServerJournalSchema);}
await SqlServerDataArchive.ImportAsync(connection,"dbo",platformArchive);
var schemas=new Dictionary<string,string>();
foreach(var snapshot in snapshots)
{
    string schema=SqlServerTenantScope.Validate("tenant_"+Regex.Replace(snapshot.Key.ToLowerInvariant(),"[^a-z0-9_]","_"));schemas[snapshot.Key]=schema;
    string scoped=SqlServerTenantScope.Connection(connection,schema);
    await SqlServerTenantScope.CreateAsync(scoped);
    await SqlServerDataArchive.ImportAsync(connection,schema,snapshot.Value);
    Console.WriteLine($"Verified {schema}: {snapshot.Value.Tables.Count} tables, {snapshot.Value.Tables.Sum(t=>t.Rows.Count)} rows");
}
using(var target=new PlatformContext(new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(connection).Options))
{
    foreach(var store in await target.Stores.ToListAsync())store.DatabaseIdentity=StoreConnections.SqlServerIdentity(SqlServerTenantScope.Connection(connection,schemas[store.ConnectionKey]));
    await target.SaveChangesAsync();
}
// Demonstrate real SQL permission boundaries, not just application-level host checks.
foreach(var entry in schemas)
{
    var options=new DbContextOptionsBuilder<ShopContext>();SqlServerTenantScope.Configure(options,SqlServerTenantScope.Connection(connection,entry.Value));
    using var tenant=new ShopContext(options.Options);int count=await tenant.Products.CountAsync();
    await tenant.Database.OpenConnectionAsync();
    foreach(string forbidden in schemas.Values.Where(s=>s!=entry.Value).Append("dbo"))
    {
        using var cmd=tenant.Database.GetDbConnection().CreateCommand();cmd.CommandText=$"SELECT COUNT(*) FROM {SqlServerTenantScope.Q(forbidden)}.{(forbidden=="dbo"?"Stores":"Products")}";
        try{await cmd.ExecuteScalarAsync();throw new InvalidOperationException("Tenant principal accessed another schema.");}
        catch(SqlException ex)when(ex.Number==229){}
    }
    Console.WriteLine("SQL permissions verified: "+entry.Value+" products="+count);
}
var overlay=new Dictionary<string,object>{["Database"]=new{Provider="SqlServer",Layout="SharedSchemas",Schema=schemas["DefaultStore"]},["Platform"]=new{DatabaseProvider="SqlServer"},["ConnectionStrings"]=new{DefaultConnection=connection,PlatformConnection=connection},["StoreConnections"]=schemas.Where(s=>s.Key!="DefaultStore").ToDictionary(s=>s.Key,s=>(object)new{Provider="SqlServer",Schema=s.Value,ConnectionString=connection})};
await File.WriteAllTextAsync(Path.Combine(folder,"sqlserver-local.json"),JsonSerializer.Serialize(overlay,new JsonSerializerOptions{WriteIndented=true}));
await File.WriteAllTextAsync(Path.Combine(folder,"verification.json"),JsonSerializer.Serialize(new{server,database=name,platformTables=platformArchive.Tables.Count,platformRows=platformArchive.Tables.Sum(t=>t.Rows.Count),stores=schemas.Select(s=>new{key=s.Key,schema=s.Value,tables=snapshots[s.Key].Tables.Count,rows=snapshots[s.Key].Tables.Sum(t=>t.Rows.Count)}),permissions="Cross-tenant and platform reads denied by SQL Server",activated=false},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("Verified transfer prepared, not activated: "+folder);
