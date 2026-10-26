using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;

namespace Shop.Api.Operations;

public static class OfflineRecovery
{
    public static FileStream AcquireInstanceLock(string contentRoot)
    {
        string directory=Path.Combine(contentRoot,"store-data");Directory.CreateDirectory(directory);
        try{return new FileStream(Path.Combine(directory,"api-instance.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
        catch(IOException){throw new InvalidOperationException("This API content root is already in use. Stop its API process before recovery or another startup.");}
    }
    public static async Task RestoreAsync(IServiceProvider services,IConfiguration config,IWebHostEnvironment environment,string id,string storeId)
    {
        var backups=services.GetRequiredService<StoreBackups>();
        // Restore rehearsal verifies every checksum before any destination is changed.
        var manifest=await backups.VerifyAsync(id);
        if(manifest.StoreId!=storeId||storeId=="platform")throw new InvalidOperationException("Confirm the exact merchant store ID belonging to this backup. Platform registry recovery uses the separate documented procedure.");
        if(manifest.Provider!="Sqlite")throw new InvalidOperationException("PostgreSQL replacement must be performed by the database operator using the verified dump; this local recovery command supports SQLite stores.");
        await using var scope=services.CreateAsyncScope();var platform=scope.ServiceProvider.GetRequiredService<PlatformContext>();
        var store=await platform.Stores.AsNoTracking().SingleAsync(s=>s.Id==storeId);
        if(store.DatabaseIdentity!=StoreConnections.DatabaseIdentity(config,store.ConnectionKey))throw new InvalidOperationException("Store database reference changed. Review recovery configuration first.");
        var source=StoreConnections.Read(config,store.ConnectionKey);if(source.Provider!="Sqlite")throw new InvalidOperationException("Provider mismatch.");
        string database=Path.GetFullPath(new SqliteConnectionStringBuilder(source.Connection).DataSource);
        string folder=Path.Combine(environment.ContentRootPath,"store-data","operations","backups",id);
        // Asset names and key IDs are immutable. Never overwrite a conflicting live file.
        foreach(var payload in manifest.Files.Where(f=>f.Path.StartsWith("uploads/")||f.Path.StartsWith("email-keys/")))
        {
            if(payload.Path.StartsWith("uploads/") && !(payload.Path.StartsWith("uploads/stores/"+storeId+"/",StringComparison.Ordinal)||store.ConnectionKey=="DefaultStore"&&!payload.Path.StartsWith("uploads/stores/",StringComparison.Ordinal)))throw new InvalidOperationException("Backup assets belong to another store.");
            string destination=payload.Path.StartsWith("uploads/")
                ?Path.Combine(environment.WebRootPath??Path.Combine(environment.ContentRootPath,"wwwroot"),payload.Path.Replace('/',Path.DirectorySeparatorChar))
                :Path.Combine(environment.ContentRootPath,"store-data",payload.Path.Replace('/',Path.DirectorySeparatorChar));
            if(File.Exists(destination)){using var stream=File.OpenRead(destination);if(Convert.ToHexString(SHA256.HashData(stream))!=payload.Sha256)throw new InvalidOperationException("A current asset or encryption key conflicts with the backup. Resolve it before recovery.");}
            NoLinks(destination);
        }
        foreach(var payload in manifest.Files.Where(f=>f.Path.StartsWith("uploads/")||f.Path.StartsWith("email-keys/")))
        {
            string destination=payload.Path.StartsWith("uploads/")?Path.Combine(environment.WebRootPath??Path.Combine(environment.ContentRootPath,"wwwroot"),payload.Path.Replace('/',Path.DirectorySeparatorChar)):Path.Combine(environment.ContentRootPath,"store-data",payload.Path.Replace('/',Path.DirectorySeparatorChar));
            if(!File.Exists(destination)){Directory.CreateDirectory(Path.GetDirectoryName(destination)!);File.Copy(Path.Combine(folder,payload.Path),destination);}
        }
        string suffix=".before-restore-"+Guid.NewGuid().ToString("N")+".db",stage=database+".restore-"+Guid.NewGuid().ToString("N")+".db";
        NoLinks(database);
        Directory.CreateDirectory(Path.GetDirectoryName(database)!);File.Copy(Path.Combine(folder,manifest.DatabaseFile),stage);
        // Rotate centrally before replacement: even a failed recovery cannot revive old sessions.
        await services.GetRequiredService<OperationJournal>().RotateSecurityVersionAsync(storeId);
        SqliteConnection.ClearAllPools();var moved=new List<(string Original,string Saved)>();bool installed=false;
        try
        {
            foreach(string path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path)){string saved=path+suffix;File.Move(path,saved);moved.Add((path,saved));}
            File.Move(stage,database);installed=true;
            using var restored=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());await restored.OpenAsync();using var command=restored.CreateCommand();command.CommandText="PRAGMA integrity_check";
            if(Convert.ToString(await command.ExecuteScalarAsync())!="ok")throw new InvalidOperationException("Post-replacement integrity check failed.");
            restored.Close();
            using var writable=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Mode=SqliteOpenMode.ReadWrite,Pooling=false}.ToString());await writable.OpenAsync();
            using var safety=writable.CreateCommand();safety.CommandText="DELETE FROM CustomerActions; UPDATE EmailOutbox SET State='RecoveryHeld',Lease='',LeaseUntilUtc='',LastError='Held after database recovery. Review before resending.' WHERE State IN ('Pending','Sending','Failed');";await safety.ExecuteNonQueryAsync();
        }
        catch
        {
            if(installed)File.Move(database,database+".failed-restore-"+Guid.NewGuid().ToString("N")+".db");
            foreach(var value in moved)File.Move(value.Saved,value.Original);throw;
        }
        await services.GetRequiredService<OperationJournal>().WriteAsync(storeId,"offline-operator","PlatformAdmin","StoreRestoredOffline","CLI",200,Guid.NewGuid().ToString("N"));
        Console.WriteLine("Store database restored. Previous database files were retained beside the original. Start the API and verify the storefront before reopening access.");
    }
    private static void NoLinks(string path){for(string? current=Path.GetFullPath(path);current is not null;current=Path.GetDirectoryName(current))if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Linked recovery paths are not supported.");}
}
