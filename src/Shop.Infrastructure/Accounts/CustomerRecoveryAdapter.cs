using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Shop.Application.Accounts;
using Shop.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Shop.BLL.Email;
using Shop.BLL.Security;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.Infrastructure.Accounts;

public sealed class CustomerRecoveryAdapter : ICustomerRecoveryStore
{
    private readonly ShopContext _db; private readonly IAccountLinkSettings _settings;
    private readonly IEmailOutbox _queue; private readonly IPasswordHasherService _passwords;
    private readonly ILogger<CustomerRecoveryAdapter> _logger; private readonly IClock _clock;
    public CustomerRecoveryAdapter(ShopContext db, IAccountLinkSettings settings, IEmailOutbox queue, IPasswordHasherService passwords, ILogger<CustomerRecoveryAdapter> logger, IClock clock)
    { _db = db; _settings = settings; _queue = queue; _passwords = passwords; _logger = logger; _clock = clock; }
    public async Task<RecoveryCustomer?> FindAsync(string email) { var c = await _db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email); return c is null ? null : new(c.Id, !string.IsNullOrWhiteSpace(c.PasswordHash)); }
    public async Task IssueAsync(int customerId, string purpose) { var c = await _db.Customers.FindAsync(customerId); if (c is not null) await IssueAsync(c, purpose); }
    public Task<bool> ConsumeAsync(string token, string purpose, string? password) => ConsumeAsync(token, purpose, password, _passwords);
    public async Task<bool> IsVerifiedAsync(int id) { var c = await _db.Customers.FindAsync(id); return c is not null && await IsVerified(c); }
    private static string Stamp(Customer customer) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(customer.PasswordHash ?? "")));
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task IssueAsync(Customer customer, string purpose)
    {
        try { await IssueCoreAsync(customer, purpose); }
        catch (Exception ex) { _logger.LogError(ex, "Customer email could not be prepared."); }
    }
    private async Task IssueCoreAsync(Customer customer, string purpose)
    {
        if (purpose == "VerifyEmail" && await IsVerified(customer)) return;
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var latest = _db.Database.GetDbConnection().CreateCommand();
            latest.CommandText = "SELECT MAX(\"ExpiresAtUtc\") FROM \"CustomerActions\" WHERE \"CustomerId\"=@id AND \"Purpose\"=@purpose";
            Add(latest, "id", customer.Id); Add(latest, "purpose", purpose);
            var previous = await latest.ExecuteScalarAsync();
            var lifetime = purpose == "ResetPassword" ? TimeSpan.FromMinutes(15) : TimeSpan.FromHours(24);
            if (previous is string value && DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiry) && expiry > _clock.UtcNow + lifetime - TimeSpan.FromMinutes(1)) return;
            string origin = _settings.Origin;
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Host != _settings.StoreHost || uri.UserInfo != "" || uri.Query != "" || uri.Fragment != "" || uri.AbsolutePath != "/" ||
                (uri.Scheme != "https" && !(_settings.AllowHttp && uri.Scheme == "http"))) throw new InvalidOperationException("Configure a trusted storefront origin for account links.");
            string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            string path = purpose == "ResetPassword" ? "reset-password" : "verify-email";
            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"CustomerActions\" (\"TokenHash\",\"CustomerId\",\"Purpose\",\"ExpiresAtUtc\",\"CredentialStamp\") VALUES ({0},{1},{2},{3},{4})", Hash(token), customer.Id, purpose, (_clock.UtcNow + lifetime).ToString("O"), Stamp(customer));
            bool arabic = _settings.Arabic;
            bool reset = purpose == "ResetPassword";
            string link = $"{origin.TrimEnd('/')}/account/{path}#token={token}";
            string body = arabic ? $"{(reset ? "استعادة كلمة المرور" : "التحقق من البريد الإلكتروني")} لمتجر {_settings.StoreName}:\n{link}\n\nهذا الرابط للاستخدام مرة واحدة وتنتهي صلاحيته خلال {(reset ? "١٥ دقيقة" : "٢٤ ساعة")}. إذا لم تطلبه، تجاهل الرسالة." : $"{(reset ? "Reset your password" : "Verify your email")} for {_settings.StoreName}:\n{link}\n\nThis link is single-use and expires in {(reset ? "15 minutes" : "24 hours")}. If you did not request this, ignore it.";
            string subject = arabic ? (reset ? "استعادة كلمة مرور المتجر" : "التحقق من بريد المتجر") : (reset ? "Reset your store password" : "Verify your store email");
            await _queue.EnqueueAsync(customer.Email, subject, body, expiresAtUtc: _clock.UtcNow + lifetime);
            await transaction.CommitAsync();
        }
        finally { await _db.Database.CloseConnectionAsync(); }
    }

    public async Task<bool> ConsumeAsync(string token, string purpose, string? password, IPasswordHasherService passwords)
    {
        if (token.Length != 43) return false;
        await _db.Database.OpenConnectionAsync();
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            using var command = _db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = "SELECT \"CustomerId\",\"ExpiresAtUtc\",\"CredentialStamp\" FROM \"CustomerActions\" WHERE \"TokenHash\"=@hash AND \"Purpose\"=@purpose";
            Add(command, "hash", Hash(token)); Add(command, "purpose", purpose);
            int id; string stamp, expires;
            using (var reader = await command.ExecuteReaderAsync())
            { if (!await reader.ReadAsync()) return false; id = reader.GetInt32(0); expires = reader.GetString(1); stamp = reader.GetString(2); }
            var customer = await _db.Customers.FindAsync(id);
            if (customer is null || Stamp(customer) != stamp || DateTime.Parse(expires, null, System.Globalization.DateTimeStyles.RoundtripKind) <= _clock.UtcNow) return false;
            int consumed = await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"CustomerActions\" WHERE \"TokenHash\"={0} AND \"Purpose\"={1}", Hash(token), purpose);
            if (consumed != 1) return false;
            if (purpose == "ResetPassword")
            {
                customer.PasswordHash = passwords.Hash(password!);
                await _db.SaveChangesAsync();
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"CustomerActions\" WHERE \"CustomerId\"={0}", id);
            }
            else await _db.Database.ExecuteSqlRawAsync(_db.Database.IsSqlServer() ? "UPDATE VerifiedCustomers WITH (UPDLOCK,SERIALIZABLE) SET Email={1} WHERE CustomerId={0}; IF @@ROWCOUNT=0 INSERT INTO VerifiedCustomers(CustomerId,Email) VALUES ({0},{1})" : "INSERT INTO \"VerifiedCustomers\" (\"CustomerId\",\"Email\") VALUES ({0},{1}) ON CONFLICT (\"CustomerId\") DO UPDATE SET \"Email\"=excluded.\"Email\"", id, customer.Email);
            await transaction.CommitAsync(); return true;
        }
        finally { await _db.Database.CloseConnectionAsync(); }
    }

    public async Task<bool> IsVerified(Customer customer)
    {
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM \"VerifiedCustomers\" WHERE \"CustomerId\"=@id AND \"Email\"=@email";
            Add(command, "id", customer.Id); Add(command, "email", customer.Email);
            return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        }
        finally { await _db.Database.CloseConnectionAsync(); }
    }
    private static void Add(System.Data.Common.DbCommand command, string name, object value) { var p = command.CreateParameter(); p.ParameterName = name; p.Value = value; command.Parameters.Add(p); }
}
