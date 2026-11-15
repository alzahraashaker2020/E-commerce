using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.DAL.Data;

namespace Shop.Api.Operations;

/// <summary>Per-store logical backups in a shared database never include another tenant's tables.</summary>
public static class SqlServerSchemaBackups
{
    public static async Task CreateAsync(string connection,string file,CancellationToken ct)
    {
        string schema=SqlServerTenantScope.Schema(connection)??throw new InvalidOperationException("Tenant schema required.");
        var archive=await SqlServerDataArchive.ExportAsync(connection,schema,ct);
        await File.WriteAllTextAsync(file,JsonSerializer.Serialize(archive),ct);
    }
    public static async Task<(int Tables,int Products,int Orders)> VerifyAsync(string connection,string file,CancellationToken ct)
    {
        var archive=JsonSerializer.Deserialize<SqlServerDataArchive.Archive>(await File.ReadAllTextAsync(file,ct))??throw new InvalidOperationException("Invalid schema archive.");
        if(archive.Schema!=SqlServerTenantScope.Schema(connection))throw new InvalidOperationException("Backup belongs to another tenant schema.");
        var builder=new SqlConnectionStringBuilder(SqlServerTenantScope.AdminConnection(connection)){InitialCatalog="matjark_restore_"+Guid.NewGuid().ToString("N")};
        string target=builder.ToString();bool created=false;
        try
        {
            using(var db=new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlServer(target).Options))
            {created=await db.Database.EnsureCreatedAsync(ct);SqlServerSchema.Apply(db,created);}
            await SqlServerDataArchive.ImportAsync(target,"dbo",archive,ct);
            using(var db=new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlServer(target).Options))
                return(archive.Tables.Count,await db.Products.CountAsync(ct),await db.Orders.CountAsync(ct));
        }
        finally
        {
            if(created)
            {
                string name=builder.InitialCatalog;builder.InitialCatalog="master";
                await using var admin=new SqlConnection(builder.ToString());await admin.OpenAsync(CancellationToken.None);
                using var cmd=new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]",admin);
                await cmd.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }
    }
}
