namespace Shop.DAL.Models;

/// <summary>
/// An RBAC role assigned to an <see cref="AdminUser"/>.
/// </summary>
public class Role
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Role name, e.g. "Admin".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Admin users assigned this role.</summary>
    public ICollection<AdminUser> AdminUsers { get; set; } = new List<AdminUser>();
}
