using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shop.Api.Tenancy;
using Shop.DAL.Data;
using Shop.SL.TokenManager;
namespace Shop.Api.Composition;

public static class AuthenticationRegistration
{
    public static IServiceCollection AddShopAuthentication(this IServiceCollection services, JwtOptions jwt)
    {
        // 5. JWT bearer authentication validated against the Jwt config section.
        string secretKey = jwt.SecretKey;
        string issuer = jwt.Issuer;
        string audience = jwt.Audience;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var user = context.Principal!;
                        var platform = context.HttpContext.RequestServices.GetRequiredService<PlatformContext>();
                        if (context.Request.Path.StartsWithSegments("/api/platform"))
                        {
                            if (!user.IsInRole("PlatformAdmin") || user.HasClaim(c => c.Type == "store_id") ||
                                !int.TryParse(user.FindFirst("actor_id")?.Value, out var id) ||
                                !await platform.Operators.AnyAsync(o => o.Id == id)) context.Fail("Invalid platform session");
                            return;
                        }
                        var scope = context.HttpContext.RequestServices.GetRequiredService<StoreScope>();
                        if (scope.Store is null || user.FindFirst("store_id")?.Value != scope.StoreId || user.IsInRole("PlatformAdmin"))
                        { context.Fail("Session belongs to another store"); return; }
                        if ((user.FindFirst("store_security_version")?.Value ?? "") != scope.SecurityVersion)
                        { context.Fail("Store session revoked after recovery"); return; }
                        if (user.IsInRole("Merchant"))
                        {
                            var db = context.HttpContext.RequestServices.GetRequiredService<ShopContext>();
                            if (!int.TryParse(user.FindFirst("actor_id")?.Value, out var id) ||
                                !await db.AdminUsers.AnyAsync(a => a.Id == id && a.IsActive && (a.Role!.Name == "Merchant" || a.Role.Name == "Admin")))
                                context.Fail("Merchant access revoked");
                        }
                        else if (user.IsInRole("Customer"))
                        {
                            var db = context.HttpContext.RequestServices.GetRequiredService<ShopContext>();
                            if (!int.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int customerId)) { context.Fail("Invalid customer"); return; }
                            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
                            string stamp = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(customer?.PasswordHash ?? "")));
                            if (customer is null || user.FindFirst("session_stamp")?.Value != stamp) context.Fail("Customer session revoked");
                        }
                    }
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });

        return services;
    }
}
