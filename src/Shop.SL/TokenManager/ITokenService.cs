using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.SL.TokenManager;

/// <summary>
/// Issues signed JWT bearer tokens for authenticated admin users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT for the supplied admin user and returns it,
    /// together with its UTC expiry, as a <see cref="TokenVM"/>.
    /// </summary>
    /// <param name="user">The authenticated admin user.</param>
    TokenVM GenerateToken(AdminUser user);
}
