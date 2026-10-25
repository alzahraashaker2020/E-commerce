using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Color"/>.
/// </summary>
public class ColorRepo : GenericRepo<Color>, IColorRepo
{
    /// <summary>
    /// Creates a new <see cref="ColorRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public ColorRepo(ShopContext context) : base(context)
    {
    }
}
