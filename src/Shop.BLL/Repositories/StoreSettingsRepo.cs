using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="StoreSettings"/>.
/// </summary>
public class StoreSettingsRepo : GenericRepo<StoreSettings>, IStoreSettingsRepo
{
    /// <summary>
    /// Creates a new <see cref="StoreSettingsRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public StoreSettingsRepo(ShopContext context) : base(context)
    {
    }
}
