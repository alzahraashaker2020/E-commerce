using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Cart"/>. Present for structural symmetry
/// only — unused by any current endpoint because the cart is client-side.
/// </summary>
public class CartRepo : GenericRepo<Cart>, ICartRepo
{
    /// <summary>
    /// Creates a new <see cref="CartRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public CartRepo(ShopContext context) : base(context)
    {
    }
}
