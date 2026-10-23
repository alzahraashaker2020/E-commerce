using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Store settings exposed to clients (the public and admin currency settings).
/// </summary>
public class StoreSettingsVM
{
    /// <summary>ISO currency code, e.g. "USD" or "EGP".</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Currency symbol, e.g. "$" or "E£".</summary>
    public string CurrencySymbol { get; set; } = string.Empty;
}

/// <summary>
/// Admin payload to update the store currency settings.
/// </summary>
public class StoreSettingsUpdateRequest
{
    /// <summary>ISO currency code, e.g. "USD" or "EGP".</summary>
    [Required]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Currency symbol, e.g. "$" or "E£".</summary>
    [Required]
    [StringLength(8)]
    public string CurrencySymbol { get; set; } = string.Empty;
}
