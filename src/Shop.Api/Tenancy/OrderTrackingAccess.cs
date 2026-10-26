using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Shop.Api.Tenancy;

/// <summary>Opaque guest access proof, scoped to this store and order.</summary>
public sealed class OrderTrackingAccess
{
    private readonly IConfiguration _configuration;
    private readonly StoreScope _store;
    public OrderTrackingAccess(IConfiguration configuration, StoreScope store) { _configuration=configuration; _store=store; }
    public string Create(string orderNumber)
    {
        using var mac = new HMACSHA256(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
        return WebEncoders.Base64UrlEncode(mac.ComputeHash(Encoding.UTF8.GetBytes("guest-tracking-v1\n"+_store.StoreId+"\n"+orderNumber)));
    }
    public bool Verify(string orderNumber, string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 100) return false;
        try { return CryptographicOperations.FixedTimeEquals(WebEncoders.Base64UrlDecode(Create(orderNumber)), WebEncoders.Base64UrlDecode(token)); }
        catch (FormatException) { return false; }
    }
}
