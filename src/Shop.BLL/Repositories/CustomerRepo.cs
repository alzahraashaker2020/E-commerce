using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Customer"/>.
/// </summary>
public class CustomerRepo : GenericRepo<Customer>, ICustomerRepo
{
    /// <summary>
    /// Creates a new <see cref="CustomerRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public CustomerRepo(ShopContext context) : base(context)
    {
    }
}
