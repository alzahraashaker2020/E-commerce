namespace Shop.DAL.ViewModels;

/// <summary>
/// Customer summary row for the admin customers list.
/// </summary>
public class CustomerListItemVM
{
    /// <summary>Customer identifier.</summary>
    public int Id { get; set; }

    /// <summary>Email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Optional phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Whether the customer opted into marketing.</summary>
    public bool MarketingConsent { get; set; }
}
