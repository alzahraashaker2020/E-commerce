namespace Shop.DAL.Models;

/// <summary>
/// A customer profile. Profile-only: no credentials are stored here (guest
/// checkout is supported, so orders may have no customer).
/// </summary>
public class Customer
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Optional phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Whether the customer opted into marketing communications.</summary>
    public bool MarketingConsent { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Orders placed by this customer.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
