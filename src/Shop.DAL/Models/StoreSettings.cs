namespace Shop.DAL.Models;

/// <summary>
/// Store-wide settings. A single row (Id = 1) holds the display currency used
/// across the storefront and admin.
/// </summary>
public class StoreSettings
{
    /// <summary>Primary key. The store uses a single row with Id = 1.</summary>
    public int Id { get; set; }

    /// <summary>ISO currency code, e.g. "USD" or "EGP".</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Currency symbol, e.g. "$" or "E£".</summary>
    public string CurrencySymbol { get; set; } = "$";

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }
}
