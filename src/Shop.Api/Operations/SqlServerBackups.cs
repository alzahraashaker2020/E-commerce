using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.SqlClient;

namespace Shop.Api.Operations;

/// <summary>Native copy-only backup and actual isolated restore, with no live database replacement.</summary>
public static class SqlServerBackups
{
    private static string Q(string name) => "["+name.Replace("]", "]]")+"]";
    private static string Literal(string value) => "N'"+value.Replace("'", "''")+"'";
    private static async Task Execute(SqlConnection db,string sql,CancellationToken ct)
    { using var cmd=new SqlCommand(sql,db){CommandTimeout=180};await cmd.ExecuteNonQueryAsync(ct); }
    private static async Task<string> NativePath(SqlConnection db,string directory,string name,CancellationToken ct)
    {
        using var service=new SqlCommand("SELECT service_account FROM sys.dm_server_services WHERE process_id=CONVERT(int,SERVERPROPERTY('ProcessID'))",db);
        string account=Convert.ToString(await service.ExecuteScalarAsync(ct))??"";
        Directory.CreateDirectory(directory);
        if(OperatingSystem.IsWindows())
        {
            if(string.IsNullOrWhiteSpace(account))throw new InvalidOperationException("SQL Server service identity is unavailable for local backup storage.");
            // Grant only the SQL Server service access to this dedicated scratch directory,
            // never to uploads, application secrets, the key ring or the rest of the workspace.
            var security=new DirectorySecurity();security.SetAccessRuleProtection(true,false);
            foreach(var principal in new[]{WindowsIdentity.GetCurrent().User!,new SecurityIdentifier(WellKnownSidType.LocalSystemSid,null)})
                security.AddAccessRule(new FileSystemAccessRule(principal,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(account,FileSystemRights.Modify,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
            new DirectoryInfo(directory).SetAccessControl(security);
        }
        return Path.Combine(directory,name);
    }
    public static async Task CreateAsync(string connection,string destination,string nativeDirectory,CancellationToken ct)
    {
        var builder=new SqlConnectionStringBuilder(connection);string name=builder.InitialCatalog;
        builder.InitialCatalog="master";builder.Pooling=false;
        await using var db=new SqlConnection(builder.ToString());await db.OpenAsync(ct);
        string native=await NativePath(db,nativeDirectory,"matjark_"+Guid.NewGuid().ToString("N")+".bak",ct);
        await Execute(db,$"BACKUP DATABASE {Q(name)} TO DISK={Literal(native)} WITH COPY_ONLY,CHECKSUM",ct);
        // SQL Server and API must share a filesystem (local installation). Never pretend a remote server path is local.
        if(!File.Exists(native))throw new InvalidOperationException("SQL Server backup is server-side. Configure shared storage before using remote SQL Server backups.");
        File.Copy(native,destination,false);
        // Keep the server's original native backup as an additional recovery copy.
    }
    public static async Task<(int Tables,int Products,int Orders)> VerifyAsync(string connection,string file,bool platform,string nativeDirectory,CancellationToken ct)
    {
        var builder=new SqlConnectionStringBuilder(connection){InitialCatalog="master",Pooling=false};
        await using var db=new SqlConnection(builder.ToString());await db.OpenAsync(ct);
        // Stage only this verified payload in SQL Server's own readable backup folder.
        string native=await NativePath(db,nativeDirectory,"matjark_check_"+Guid.NewGuid().ToString("N")+".bak",ct);
        File.Copy(file,native,false);
        await Execute(db,$"RESTORE VERIFYONLY FROM DISK={Literal(native)} WITH CHECKSUM",ct);
        var files=new List<(string Name,string Type)>();
        using(var cmd=new SqlCommand($"RESTORE FILELISTONLY FROM DISK={Literal(native)}",db))
        {using var reader=await cmd.ExecuteReaderAsync(ct);while(await reader.ReadAsync(ct))files.Add((reader.GetString(reader.GetOrdinal("LogicalName")),reader.GetString(reader.GetOrdinal("Type"))));}
        using var paths=new SqlCommand("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath')),CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'))",db);
        string data,log;using(var r=await paths.ExecuteReaderAsync(ct)){await r.ReadAsync(ct);data=r.GetString(0);log=r.GetString(1);}
        string name="matjark_restore_"+Guid.NewGuid().ToString("N");bool created=false;
        try
        {
            string moves=string.Join(",",files.Select((f,i)=>$"MOVE {Literal(f.Name)} TO {Literal(Path.Combine(f.Type=="L"?log:data,name+"_"+i+(f.Type=="L"?".ldf":".mdf")))}"));
            await Execute(db,$"RESTORE DATABASE {Q(name)} FROM DISK={Literal(native)} WITH {moves},RECOVERY,CHECKSUM",ct);created=true;
            builder.InitialCatalog=name;
            await using var restored=new SqlConnection(builder.ToString());await restored.OpenAsync(ct);
            // Unlike VERIFYONLY, this checks a restored database and its logical constraints.
            using(var integrity=new SqlCommand("DBCC CHECKDB WITH NO_INFOMSGS, ALL_ERRORMSGS",restored){CommandTimeout=180})
            using(var errors=await integrity.ExecuteReaderAsync(ct)){do{if(await errors.ReadAsync(ct))throw new InvalidOperationException("SQL Server restore integrity check failed.");}while(await errors.NextResultAsync(ct));}
            async Task<int> Count(string table){using var cmd=new SqlCommand("SELECT COUNT(*) FROM "+table,restored);return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));}
            return(await Count("sys.tables"),platform?0:await Count("Products"),platform?0:await Count("Orders"));
        }
        finally
        {
            // Only the random database created above is ever dropped; no REPLACE or live restore.
            if(created)await Execute(db,$"ALTER DATABASE {Q(name)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {Q(name)}",CancellationToken.None);
        }
    }
}
