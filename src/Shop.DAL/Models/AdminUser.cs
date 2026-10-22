namespace Shop.DAL.Models;

/// <summary>
/// A back-office administrator account. This is a plain DAL entity: it holds
/// only the precomputed <see cref="PasswordHash"/> and carries no hashing
/// logic. Hashing/verification lives in the business layer.
/// </summary>
public class AdminUser
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Unique login username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Precomputed password hash (never plaintext).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Foreign key to the assigned <see cref="Role"/>.</summary>
    public int RoleId { get; set; }

    /// <summary>Whether the account is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Assigned role navigation.</summary>
    public Role? Role { get; set; }
}
