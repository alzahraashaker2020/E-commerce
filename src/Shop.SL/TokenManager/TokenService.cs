using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;
using Shop.DAL.Data;

namespace Shop.SL.TokenManager;

/// <summary>
/// JWT token service. Reads the <c>Jwt</c> configuration section
/// (SecretKey/Issuer/Audience/ExpiryMinutes), builds the admin claim set, and
/// signs a bearer token with HMAC-SHA256.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IStoreIdentity _store;

    /// <summary>
    /// Creates a new <see cref="TokenService"/>.
    /// </summary>
    /// <param name="configuration">Application configuration (supplies the Jwt section).</param>
    public TokenService(IConfiguration configuration, IStoreIdentity store)
    {
        _configuration = configuration;
        _store = store;
    }

    /// <inheritdoc />
    public TokenVM GenerateToken(AdminUser user)
    {
        // Read the JWT settings from the dedicated "Jwt" config section.
        string secretKey = _configuration["Jwt:SecretKey"]!;
        string issuer = _configuration["Jwt:Issuer"]!;
        string audience = _configuration["Jwt:Audience"]!;
        int expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "120");

        DateTime expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        // Claims: Sub = username (the token subject), Role = role name (drives
        // [Authorize(Roles=...)] checks), NameId = the admin user's id. These
        // are the minimum needed to identify and authorize the admin.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(ClaimTypes.Role, "Merchant"),
            new("store_id", _store.StoreId),
            new("store_security_version", _store.SecurityVersion),
            new("actor_id", user.Id.ToString()),
            new(JwtRegisteredClaimNames.NameId, user.Id.ToString())
        };

        // Sign with a symmetric key using HMAC-SHA256; the same secret is used
        // by the API to validate the signature on incoming requests.
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        string encoded = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenVM
        {
            Token = encoded,
            Expires = expires
        };
    }

    /// <inheritdoc />
    public TokenVM GenerateCustomerToken(Customer customer)
    {
        // Same signing settings as the admin token; only the claim set differs.
        string secretKey = _configuration["Jwt:SecretKey"]!;
        string issuer = _configuration["Jwt:Issuer"]!;
        string audience = _configuration["Jwt:Audience"]!;
        int expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "120");

        DateTime expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        // Sub = customer id, Email claim for convenience, Role = "Customer" so
        // customer tokens never satisfy admin-only [Authorize] checks.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, customer.Email),
            new(ClaimTypes.Role, "Customer"),
            new("session_stamp", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(customer.PasswordHash ?? "")))),
            new("store_id", _store.StoreId),
            new("store_security_version", _store.SecurityVersion),
            new(JwtRegisteredClaimNames.NameId, customer.Id.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        string encoded = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenVM
        {
            Token = encoded,
            Expires = expires
        };
    }
    public TokenVM GeneratePlatformToken(int id, string username)
    {
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "120"));
        var token = new JwtSecurityToken(issuer: _configuration["Jwt:Issuer"], audience: _configuration["Jwt:Audience"],
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, username), new Claim("actor_id", id.ToString()), new Claim(ClaimTypes.Role, "PlatformAdmin") },
            expires: expires, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!)), SecurityAlgorithms.HmacSha256));
        return new TokenVM { Token = new JwtSecurityTokenHandler().WriteToken(token), Expires = expires };
    }
}
