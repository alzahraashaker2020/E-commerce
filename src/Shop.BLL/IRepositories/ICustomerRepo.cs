using Shop.DAL.Models;

namespace Shop.BLL.IRepositories;

/// <summary>
/// Repository contract for <see cref="Customer"/>, with account lookups used by
/// customer registration and login (local + social).
/// </summary>
public interface ICustomerRepo : IGenericRepo<Customer>
{
    /// <summary>Finds a customer by email (case-insensitive), or null.</summary>
    Task<Customer?> GetByEmailAsync(string email);

    /// <summary>Finds a customer by external provider identity, or null.</summary>
    Task<Customer?> GetByExternalAsync(string provider, string externalId);
}
