using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shop.Api.Tenancy;

namespace Shop.Api.Operations;

/// <summary>Private snapshots and isolated restore rehearsals. No live database is overwritten.</summary>
public sealed class StoreBackups
{
    private readonly IServiceScopeFactory _scopes;private readonly IConfiguration _config;private readonly IWebHostEnvironment _environment;
    private readonly SemaphoreSlim _gate=new(1,1);
    private readonly JsonSerializerOptions _json=new(JsonSerializerDefaults.Web){WriteIndented=true};
    public StoreBackups(IServiceScopeFactory scopes,IConfiguration config,IWebHostEnvironment environment){_scopes=scopes;_config=config;_environment=environment;}
    private string Root=>Path.Combine(_environment.ContentRootPath,"store-data","operations","backups");
    private string Folder(string id){if(!Guid.TryParseExact(id,"N",out _))throw new InvalidOperationException("Invalid backup ID.");return Path.Combine(Root,id);}
    public List<Manifest> List(string? store=null)
    {
        if(!Directory.Exists(Root))return new();
        var rows=new List<Manifest>();
        foreach(var directory in Directory.EnumerateDirectories(Root))
        {
            if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0||!Guid.TryParseExact(Path.GetFileName(directory),"N",out _))continue;
            string path=Path.Combine(directory,"manifest.json");if(!File.Exists(path))continue;
            try{using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);var value=JsonSerializer.Deserialize<Manifest>(stream,_json)??throw new JsonException();if(value.Id!=Path.GetFileName(directory))throw new JsonException();if(store is null||value.StoreId==store)rows.Add(value);}
            catch(Exception ex)when(ex is IOException or JsonException){if(store is null)rows.Add(new Manifest{Id=Path.GetFileName(directory),StoreName="Unreadable snapshot manifest",Provider="Unknown",LastVerificationState="Failed",CreatedAtUtc=Directory.GetCreationTimeUtc(directory)});}
        }
        return rows.OrderByDescending(m=>m.CreatedAtUtc).Take(100).ToList();
    }
    public async Task<Manifest> CreateAsync(string storeId,CancellationToken cancellation=default)
    {
        if(!await _gate.WaitAsync(0,cancellation))throw new InvalidOperationException("Another backup or restore check is running. Try again shortly.");
        string? folder=null;
        try
        {
            await using var scope=_scopes.CreateAsyncScope();var registry=scope.ServiceProvider.GetRequiredService<PlatformContext>();
            var store=storeId=="platform"?null:await registry.Stores.AsNoTracking().SingleOrDefaultAsync(s=>s.Id==storeId,cancellation);
            if(storeId!="platform"&&(store is null||store.State is not ("Active" or "Suspended")))throw new InvalidOperationException("Select a ready store.");
            var source=store is null?("Sqlite",registry.Database.GetConnectionString()!):StoreConnections.Read(_config,store.ConnectionKey);
            ProtectOperationsRoot();
            string id=Guid.NewGuid().ToString("N");folder=Folder(id);Directory.CreateDirectory(folder);
            string databaseFile=source.Item1=="Postgres"?"database.dump":"database.sqlite";
            if(source.Item1=="Postgres")await PgTool("pg_dump",source.Item2,new[]{"--format=custom","--no-owner","--no-privileges","--file",Path.Combine(folder,databaseFile)},cancellation);
            else
            {
                var builder=new SqliteConnectionStringBuilder(source.Item2){Mode=SqliteOpenMode.ReadOnly};
                using var connection=new SqliteConnection(builder.ToString());await connection.OpenAsync(cancellation);
                using var destination=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(folder,databaseFile),Pooling=false}.ToString());await destination.OpenAsync(cancellation);connection.BackupDatabase(destination);
            }
            if(store is not null)
            {
                string uploads=Path.Combine(_environment.WebRootPath??Path.Combine(_environment.ContentRootPath,"wwwroot"),"uploads");
                CopyTree(Path.Combine(uploads,"stores",store.Id),Path.Combine(folder,"uploads","stores",store.Id));
                if(store.ConnectionKey=="DefaultStore")CopyTree(uploads,Path.Combine(folder,"uploads"),skipStores:true);
            }
            CopyTree(Path.Combine(_environment.ContentRootPath,"store-data","email-keys"),Path.Combine(folder,"email-keys"));
            var manifest=new Manifest{Id=id,StoreId=storeId,StoreName=store?.Name??"Platform registry",Provider=source.Item1,CreatedAtUtc=DateTime.UtcNow,DatabaseFile=databaseFile};
            foreach(var file in Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories))
            {using var stream=File.OpenRead(file);manifest.Files.Add(new(Path.GetRelativePath(folder,file).Replace('\\','/'),stream.Length,Convert.ToHexString(SHA256.HashData(stream))));}
            manifest.Bytes=manifest.Files.Sum(f=>f.Bytes);Save(manifest);return manifest;
        }
        catch{if(folder is not null)File.WriteAllText(Path.Combine(folder,"incomplete.txt"),"Backup did not complete. Do not use this directory for recovery.");throw;}
        finally{_gate.Release();}
    }
    public async Task<Manifest> VerifyAsync(string id,CancellationToken cancellation=default)
    {
        if(!await _gate.WaitAsync(0,cancellation))throw new InvalidOperationException("Another backup or restore check is running. Try again shortly.");
        Manifest? checking=null;
        try
        {
            string folder=Folder(id);NoLinks(folder);string manifestPath=Path.Combine(folder,"manifest.json");
            if(!File.Exists(manifestPath))throw new InvalidOperationException("Backup not found or incomplete.");
            var manifest=JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath),_json)??throw new InvalidOperationException("Invalid backup manifest.");
            if(manifest.Id!=id||manifest.DatabaseFile is not ("database.sqlite" or "database.dump"))throw new InvalidOperationException("Invalid backup identity.");
            checking=manifest;
            foreach(var file in manifest.Files)
            {
                string path=SafeFile(folder,file.Path);NoLinks(path);
                using var stream=File.OpenRead(path);
                if(stream.Length!=file.Bytes||Convert.ToHexString(SHA256.HashData(stream))!=file.Sha256)throw new InvalidOperationException("Backup checksum mismatch. Recovery check was stopped.");
            }
            if(!manifest.Files.Any(f=>f.Path==manifest.DatabaseFile))throw new InvalidOperationException("Database payload is missing from the manifest.");
            string check=Path.Combine(_environment.ContentRootPath,"store-data","operations","restore-checks",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(check);
            if(manifest.Provider=="Sqlite")
            {
                CopyTree(folder,check);
                using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(check,manifest.DatabaseFile),Mode=SqliteOpenMode.ReadOnly}.ToString());await db.OpenAsync(cancellation);
                using var cmd=db.CreateCommand();cmd.CommandText="PRAGMA integrity_check";if(Convert.ToString(await cmd.ExecuteScalarAsync(cancellation))!="ok")throw new InvalidOperationException("Restored database integrity check failed.");
                cmd.CommandText="PRAGMA foreign_key_check";using(var reader=await cmd.ExecuteReaderAsync(cancellation)){if(await reader.ReadAsync(cancellation))throw new InvalidOperationException("Restored database has broken foreign keys.");}
                cmd.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table'";manifest.Tables=Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellation));
                if(manifest.StoreId!="platform"){cmd.CommandText="SELECT COUNT(*) FROM Products";manifest.Products=Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellation));cmd.CommandText="SELECT COUNT(*) FROM Orders";manifest.Orders=Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellation));}
            }
            else if(manifest.Provider=="Postgres")
            {
                await VerifyPostgres(manifest,folder,cancellation);
                CopyTree(Path.Combine(folder,"uploads"),Path.Combine(check,"uploads"));CopyTree(Path.Combine(folder,"email-keys"),Path.Combine(check,"email-keys"));
            }
            else throw new InvalidOperationException("Unsupported backup provider.");
            manifest.LastVerifiedAtUtc=DateTime.UtcNow;manifest.LastVerificationState="Passed";manifest.RestoreCheckId=Path.GetFileName(check);Save(manifest);return manifest;
        }
        catch{if(checking is not null){checking.LastVerificationState="Failed";Save(checking);}throw;}
        finally{_gate.Release();}
    }
    private async Task VerifyPostgres(Manifest manifest,string folder,CancellationToken cancellation)
    {
        await using var scope=_scopes.CreateAsyncScope();var registry=scope.ServiceProvider.GetRequiredService<PlatformContext>();var store=await registry.Stores.FindAsync(new object[]{manifest.StoreId},cancellation)??throw new InvalidOperationException("Store registration required for restore validation.");
        var source=StoreConnections.Read(_config,store.ConnectionKey);if(source.Provider!="Postgres")throw new InvalidOperationException("Store provider changed; review recovery configuration.");
        var admin=new NpgsqlConnectionStringBuilder(source.Connection){Database="postgres",Pooling=false};
        await using var connection=new NpgsqlConnection(admin.ToString());await connection.OpenAsync(cancellation);
        string name="shop_restore_"+Guid.NewGuid().ToString("N");bool created=false;
        try
        {
            await using(var create=new NpgsqlCommand("CREATE DATABASE "+name,connection)){await create.ExecuteNonQueryAsync(cancellation);created=true;}
            var target=new NpgsqlConnectionStringBuilder(source.Connection){Database=name,Pooling=false};
            await PgTool("pg_restore",target.ToString(),new[]{"--no-owner","--no-privileges","--exit-on-error","--dbname",name,Path.Combine(folder,manifest.DatabaseFile)},cancellation);
            await using var restored=new NpgsqlConnection(target.ToString());await restored.OpenAsync(cancellation);
            await using var cmd=new NpgsqlCommand("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public'",restored);manifest.Tables=Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellation));
            cmd.CommandText="SELECT COUNT(*) FROM \"Products\"";manifest.Products=Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellation));cmd.CommandText="SELECT COUNT(*) FROM \"Orders\"";manifest.Orders=Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellation));
        }
        finally{if(created){await using var drop=new NpgsqlCommand("DROP DATABASE "+name+" WITH (FORCE)",connection);await drop.ExecuteNonQueryAsync(CancellationToken.None);}}
    }
    private async Task PgTool(string name,string connection,IEnumerable<string> arguments,CancellationToken cancellation)
    {
        var value=new NpgsqlConnectionStringBuilder(connection);string bin=_config["Operations:PostgresBin"]??"";
        string executable=name+(OperatingSystem.IsWindows()?".exe":"");
        var start=new ProcessStartInfo{FileName=string.IsNullOrEmpty(bin)?executable:Path.Combine(Path.GetFullPath(bin,_environment.ContentRootPath),executable),UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
        foreach(string key in start.Environment.Keys.Where(k=>k.StartsWith("PG",StringComparison.OrdinalIgnoreCase)).ToArray())start.Environment.Remove(key);
        foreach(string argument in arguments)start.ArgumentList.Add(argument);
        start.Environment["PGHOST"]=value.Host;start.Environment["PGPORT"]=value.Port.ToString();start.Environment["PGDATABASE"]=value.Database;start.Environment["PGUSER"]=value.Username;start.Environment["PGPASSWORD"]=value.Password;start.Environment["PGCONNECT_TIMEOUT"]="15";
        start.Environment["PGSSLMODE"]=value.SslMode switch{SslMode.Disable=>"disable",SslMode.Require=>"require",SslMode.VerifyCA=>"verify-ca",SslMode.VerifyFull=>"verify-full",_=>"prefer"};
        if(!string.IsNullOrEmpty(value.RootCertificate))start.Environment["PGSSLROOTCERT"]=value.RootCertificate;
        using var process=Process.Start(start)??throw new InvalidOperationException("PostgreSQL backup tools are unavailable.");
        var stderr=process.StandardError.ReadToEndAsync();var stdout=process.StandardOutput.ReadToEndAsync();using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(TimeSpan.FromMinutes(2));
        try{await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(entireProcessTree:true);throw;}
        await Task.WhenAll(stderr,stdout);if(process.ExitCode!=0)throw new InvalidOperationException("PostgreSQL backup/restore failed. Check tool version, permissions and server configuration.");
    }
    private void Save(Manifest manifest){string path=Path.Combine(Folder(manifest.Id),"manifest.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(manifest,_json));File.Move(path+".tmp",path,true);}
    private void ProtectOperationsRoot()
    {
        string path=Path.GetDirectoryName(Root)!;Directory.CreateDirectory(path);NoLinks(path);
        if(OperatingSystem.IsWindows())
        {
            var acl=new DirectorySecurity();acl.SetAccessRuleProtection(true,false);
            foreach(var identity in new[]{WindowsIdentity.GetCurrent().User!,new SecurityIdentifier("S-1-5-18")})
                acl.AddAccessRule(new FileSystemAccessRule(identity,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
            new DirectoryInfo(path).SetAccessControl(acl);
        }
        else File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
    }
    private static string SafeFile(string root,string relative){if(Path.IsPathRooted(relative)||relative.Replace('\\','/').Split('/').Any(p=>p is "" or "." or ".."))throw new InvalidOperationException("Unsafe backup path.");string path=Path.GetFullPath(Path.Combine(root,relative));if(!path.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unsafe backup path.");return path;}
    private static void NoLinks(string path){for(string? current=Path.GetFullPath(path);current is not null;current=Path.GetDirectoryName(current)){if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Linked backup paths are not supported.");}}
    private static void CopyTree(string source,string destination,bool skipStores=false)
    {
        if(!Directory.Exists(source))return;NoLinks(source);Directory.CreateDirectory(destination);
        foreach(string file in Directory.EnumerateFiles(source)){NoLinks(file);File.Copy(file,Path.Combine(destination,Path.GetFileName(file)),false);}
        foreach(string child in Directory.EnumerateDirectories(source)){if(skipStores&&Path.GetFileName(child)=="stores")continue;CopyTree(child,Path.Combine(destination,Path.GetFileName(child)));}
    }
    public sealed class Manifest
    {
        public string Id{get;set;}="";public string StoreId{get;set;}="";public string StoreName{get;set;}="";public string Provider{get;set;}="";public string DatabaseFile{get;set;}="";
        public DateTime CreatedAtUtc{get;set;}public DateTime? LastVerifiedAtUtc{get;set;}public string? RestoreCheckId{get;set;}public long Bytes{get;set;}public int? Tables{get;set;}public int? Products{get;set;}public int? Orders{get;set;}public List<Payload> Files{get;set;}=new();
        public string LastVerificationState{get;set;}="NotChecked";
    }
    public sealed record Payload(string Path,long Bytes,string Sha256);
}
