using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Role"/>.
/// </summary>
public class RoleRepo : GenericRepo<Role>, IRoleRepo
{
    /// <summary>
    /// Creates a new <see cref="RoleRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public RoleRepo(ShopContext context) : base(context)
    {
    }
}
