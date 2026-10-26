using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Api.Tenancy;
using Shop.BLL.Email;
using Shop.BLL.Security;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.Api.Security;

public sealed class CustomerEmailSecurity
{
    private readonly IHttpContextAccessor _http;
    private readonly ShopContext _db;
    private readonly StoreScope _scope;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailOutbox _queue;
    private readonly ILogger<CustomerEmailSecurity> _logger;
    public CustomerEmailSecurity(ShopContext db,StoreScope scope,IConfiguration config,IWebHostEnvironment environment,IEmailOutbox queue,ILogger<CustomerEmailSecurity> logger,IHttpContextAccessor http)
    {_http=http;_db=db;_scope=scope;_config=config;_environment=environment;_queue=queue;_logger=logger;}
    private static string Stamp(Customer customer)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(customer.PasswordHash??"")));
    private static string Hash(string token)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task IssueAsync(Customer customer,string purpose)
    {
        try{await IssueCoreAsync(customer,purpose);}
        catch(Exception ex){_logger.LogError(ex,"Customer email could not be prepared.");}
    }
    private async Task IssueCoreAsync(Customer customer,string purpose)
    {
        if(purpose=="VerifyEmail" && await IsVerified(customer)) return;
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var latest=_db.Database.GetDbConnection().CreateCommand();
            latest.CommandText="SELECT MAX(\"ExpiresAtUtc\") FROM \"CustomerActions\" WHERE \"CustomerId\"=@id AND \"Purpose\"=@purpose";
            Add(latest,"id",customer.Id);Add(latest,"purpose",purpose);
            var previous=await latest.ExecuteScalarAsync();
            var lifetime=purpose=="ResetPassword"?TimeSpan.FromMinutes(15):TimeSpan.FromHours(24);
            if(previous is string value && DateTime.TryParse(value,null,System.Globalization.DateTimeStyles.RoundtripKind,out var expiry) && expiry>DateTime.UtcNow+lifetime-TimeSpan.FromMinutes(1)) return;
            string origin=_config[$"StoreLinks:{_scope.StoreId}:Origin"]??$"{(_environment.IsDevelopment()?"http":"https")}://{_scope.Store!.Host}{(_environment.IsDevelopment()?":4200":"")}";
            if(!Uri.TryCreate(origin,UriKind.Absolute,out var uri)||uri.Host!=_scope.Store!.Host||uri.UserInfo!=""||uri.Query!=""||uri.Fragment!=""||uri.AbsolutePath!="/"||
                (uri.Scheme!="https"&&!(_environment.IsDevelopment()&&uri.Scheme=="http"))) throw new InvalidOperationException("Configure a trusted storefront origin for account links.");
            string token=WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            string path=purpose=="ResetPassword"?"reset-password":"verify-email";
            await using var transaction=await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"CustomerActions\" (\"TokenHash\",\"CustomerId\",\"Purpose\",\"ExpiresAtUtc\",\"CredentialStamp\") VALUES ({0},{1},{2},{3},{4})",Hash(token),customer.Id,purpose,(DateTime.UtcNow+lifetime).ToString("O"),Stamp(customer));
            bool arabic=_http.HttpContext?.Request.Headers.AcceptLanguage.ToString().Split(',')[0].Split(';')[0].Split('-')[0].Trim().ToLowerInvariant()=="ar";
            bool reset=purpose=="ResetPassword";
            string link=$"{origin.TrimEnd('/')}/account/{path}#token={token}";
            string body=arabic?$"{(reset?"استعادة كلمة المرور":"التحقق من البريد الإلكتروني")} لمتجر {_scope.Store.Name}:\n{link}\n\nهذا الرابط للاستخدام مرة واحدة وتنتهي صلاحيته خلال {(reset?"١٥ دقيقة":"٢٤ ساعة")}. إذا لم تطلبه، تجاهل الرسالة.":$"{(reset?"Reset your password":"Verify your email")} for {_scope.Store.Name}:\n{link}\n\nThis link is single-use and expires in {(reset?"15 minutes":"24 hours")}. If you did not request this, ignore it.";
            string subject=arabic?(reset?"استعادة كلمة مرور المتجر":"التحقق من بريد المتجر"):(reset?"Reset your store password":"Verify your store email");
            await _queue.EnqueueAsync(customer.Email,subject,body,expiresAtUtc:DateTime.UtcNow+lifetime);
            await transaction.CommitAsync();
        }
        finally {await _db.Database.CloseConnectionAsync();}
    }

    public async Task<bool> ConsumeAsync(string token,string purpose,string? password,IPasswordHasherService passwords)
    {
        if(token.Length!=43)return false;
        await _db.Database.OpenConnectionAsync();
        try
        {
            await using var transaction=await _db.Database.BeginTransactionAsync();
            using var command=_db.Database.GetDbConnection().CreateCommand();
            command.Transaction=transaction.GetDbTransaction();
            command.CommandText="SELECT \"CustomerId\",\"ExpiresAtUtc\",\"CredentialStamp\" FROM \"CustomerActions\" WHERE \"TokenHash\"=@hash AND \"Purpose\"=@purpose";
            Add(command,"hash",Hash(token));Add(command,"purpose",purpose);
            int id;string stamp,expires;
            using(var reader=await command.ExecuteReaderAsync())
            {if(!await reader.ReadAsync())return false;id=reader.GetInt32(0);expires=reader.GetString(1);stamp=reader.GetString(2);}
            var customer=await _db.Customers.FindAsync(id);
            if(customer is null||Stamp(customer)!=stamp||DateTime.Parse(expires,null,System.Globalization.DateTimeStyles.RoundtripKind)<=DateTime.UtcNow)return false;
            int consumed=await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"CustomerActions\" WHERE \"TokenHash\"={0} AND \"Purpose\"={1}",Hash(token),purpose);
            if(consumed!=1)return false;
            if(purpose=="ResetPassword")
            {
                customer.PasswordHash=passwords.Hash(password!);
                await _db.SaveChangesAsync();
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"CustomerActions\" WHERE \"CustomerId\"={0}",id);
            }
            else await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"VerifiedCustomers\" (\"CustomerId\",\"Email\") VALUES ({0},{1}) ON CONFLICT (\"CustomerId\") DO UPDATE SET \"Email\"=excluded.\"Email\"",id,customer.Email);
            await transaction.CommitAsync();return true;
        }
        finally{await _db.Database.CloseConnectionAsync();}
    }

    public async Task<bool> IsVerified(Customer customer)
    {
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var command=_db.Database.GetDbConnection().CreateCommand();
            command.CommandText="SELECT COUNT(*) FROM \"VerifiedCustomers\" WHERE \"CustomerId\"=@id AND \"Email\"=@email";
            Add(command,"id",customer.Id);Add(command,"email",customer.Email);
            return Convert.ToInt64(await command.ExecuteScalarAsync())==1;
        }
        finally{await _db.Database.CloseConnectionAsync();}
    }
    private static void Add(System.Data.Common.DbCommand command,string name,object value){var p=command.CreateParameter();p.ParameterName=name;p.Value=value;command.Parameters.Add(p);}
}
