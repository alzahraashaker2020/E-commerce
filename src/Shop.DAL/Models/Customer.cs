namespace Shop.DAL.Models;

/// <summary>
/// A customer profile and (optionally) account.
///
/// Guest checkout is still supported, so an order may have no customer. A
/// registered customer additionally carries credentials: either a local
/// password hash (<see cref="PasswordHash"/>) or an external social identity
/// (<see cref="ExternalProvider"/> + <see cref="ExternalId"/>), or both.
/// </summary>
public class Customer
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Email address (unique for registered accounts).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Optional phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Whether the customer opted into marketing communications.</summary>
    public bool MarketingConsent { get; set; }

    /// <summary>
    /// Local password hash. Null for accounts created purely via social login
    /// (a guest-only profile row also has no hash).
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// External identity provider name when the account was created/linked via
    /// social login (e.g. "Google" or "Facebook"); null for local accounts.
    /// </summary>
    public string? ExternalProvider { get; set; }

    /// <summary>External provider subject id, paired with <see cref="ExternalProvider"/>.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Whether this profile has registered account credentials.</summary>
    public bool IsRegistered { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Orders placed by this customer.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>Reviews written by this customer.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
