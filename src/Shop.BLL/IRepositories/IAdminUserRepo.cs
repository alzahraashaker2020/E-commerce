using Shop.DAL.Models;

namespace Shop.BLL.IRepositories;

/// <summary>
/// Repository contract for <see cref="AdminUser"/>, adding username lookup.
/// </summary>
public interface IAdminUserRepo : IGenericRepo<AdminUser>
{
    /// <summary>
    /// Loads an admin user by username, including the assigned role, or
    /// <c>null</c> when no account matches.
    /// </summary>
    /// <param name="username">The login username.</param>
    Task<AdminUser?> GetByUsernameAsync(string username);
}
