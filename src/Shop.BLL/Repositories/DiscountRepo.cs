using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Discount"/>, implementing code lookup.
/// </summary>
public class DiscountRepo : GenericRepo<Discount>, IDiscountRepo
{
    /// <summary>
    /// Creates a new <see cref="DiscountRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public DiscountRepo(ShopContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<Discount?> GetByCodeAsync(string code)
    {
        return _context.Discounts.FirstOrDefaultAsync(d => d.Code == code);
    }
}
