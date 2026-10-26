using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.SL.TokenManager;

/// <summary>
/// Issues signed JWT bearer tokens for authenticated admin users.
/// </summary>
public interface ITokenService
{
    TokenVM GeneratePlatformToken(int id, string username);
    /// <summary>
    /// Generates a signed JWT for the supplied admin user and returns it,
    /// together with its UTC expiry, as a <see cref="TokenVM"/>.
    /// </summary>
    /// <param name="user">The authenticated admin user.</param>
    TokenVM GenerateToken(AdminUser user);

    /// <summary>
    /// Generates a signed JWT for an authenticated customer. The token carries
    /// the "Customer" role claim and the customer's id as the subject, keeping
    /// customer tokens distinct from admin tokens.
    /// </summary>
    /// <param name="customer">The authenticated customer.</param>
    TokenVM GenerateCustomerToken(Customer customer);
}
