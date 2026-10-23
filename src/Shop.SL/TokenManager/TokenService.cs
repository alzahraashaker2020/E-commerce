using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.SL.TokenManager;

/// <summary>
/// JWT token service. Reads the <c>Jwt</c> configuration section
/// (SecretKey/Issuer/Audience/ExpiryMinutes), builds the admin claim set, and
/// signs a bearer token with HMAC-SHA256.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Creates a new <see cref="TokenService"/>.
    /// </summary>
    /// <param name="configuration">Application configuration (supplies the Jwt section).</param>
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
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
            new(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
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
}
