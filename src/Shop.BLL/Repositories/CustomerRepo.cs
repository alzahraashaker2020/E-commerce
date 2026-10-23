using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Customer"/>, implementing the account
/// lookups used by customer registration and login (local + social).
/// </summary>
public class CustomerRepo : GenericRepo<Customer>, ICustomerRepo
{
    /// <summary>
    /// Creates a new <see cref="CustomerRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public CustomerRepo(ShopContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<Customer?> GetByEmailAsync(string email)
    {
        // Case-insensitive email match so logins are not case-sensitive.
        string normalized = email.Trim().ToLower();
        return _context.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == normalized);
    }

    /// <inheritdoc />
    public Task<Customer?> GetByExternalAsync(string provider, string externalId)
    {
        return _context.Customers
            .FirstOrDefaultAsync(c => c.ExternalProvider == provider && c.ExternalId == externalId);
    }
}
