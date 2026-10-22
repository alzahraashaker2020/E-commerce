using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="InventoryItem"/>.
/// </summary>
public class InventoryItemRepo : GenericRepo<InventoryItem>, IInventoryItemRepo
{
    /// <summary>
    /// Creates a new <see cref="InventoryItemRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public InventoryItemRepo(ShopContext context) : base(context)
    {
    }
}
