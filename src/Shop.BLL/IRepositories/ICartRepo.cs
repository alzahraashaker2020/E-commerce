using Shop.DAL.Models;

namespace Shop.BLL.IRepositories;

/// <summary>
/// Repository contract for <see cref="Cart"/>. Kept for structural symmetry
/// only: the cart is client-side in this scaffold and no current endpoint uses
/// this repo.
/// </summary>
public interface ICartRepo : IGenericRepo<Cart>
{
}
