using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="AdminUser"/>, implementing username lookup.
/// </summary>
public class AdminUserRepo : GenericRepo<AdminUser>, IAdminUserRepo
{
    /// <summary>
    /// Creates a new <see cref="AdminUserRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public AdminUserRepo(ShopContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<AdminUser?> GetByUsernameAsync(string username)
    {
        // Include the role so the token service can emit the role claim.
        return _context.AdminUsers
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == username);
    }
}
