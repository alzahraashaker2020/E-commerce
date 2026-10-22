using Shop.DAL.Models;

namespace Shop.BLL.IRepositories;

/// <summary>
/// Repository contract for <see cref="Discount"/>, adding code lookup.
/// </summary>
public interface IDiscountRepo : IGenericRepo<Discount>
{
    /// <summary>
    /// Loads a discount by its unique code, or <c>null</c> when none matches.
    /// </summary>
    /// <param name="code">The discount code.</param>
    Task<Discount?> GetByCodeAsync(string code);
}
