using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Category"/>.
/// </summary>
public class CategoryRepo : GenericRepo<Category>, ICategoryRepo
{
    /// <summary>
    /// Creates a new <see cref="CategoryRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public CategoryRepo(ShopContext context) : base(context)
    {
    }
}
