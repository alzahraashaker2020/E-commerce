using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Size"/>.
/// </summary>
public class SizeRepo : GenericRepo<Size>, ISizeRepo
{
    /// <summary>
    /// Creates a new <see cref="SizeRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public SizeRepo(ShopContext context) : base(context)
    {
    }
}
