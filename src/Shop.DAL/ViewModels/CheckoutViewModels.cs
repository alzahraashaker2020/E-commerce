using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Guest checkout request. Totals are computed server-side from trusted prices.
/// </summary>
public class PlaceOrderRequest
{
    /// <summary>Guest name (required, 1-120 chars).</summary>
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string GuestName { get; set; } = string.Empty;

    /// <summary>Guest email (required, valid email).</summary>
    [Required]
    [EmailAddress]
    public string GuestEmail { get; set; } = string.Empty;

    /// <summary>Ordered lines (at least one required).</summary>
    [Required]
    [MinLength(1)]
    public List<CartLineRequest> Lines { get; set; } = new List<CartLineRequest>();

    /// <summary>Optional discount code (max 40 chars).</summary>
    [StringLength(40)]
    public string? DiscountCode { get; set; }
}

/// <summary>
/// Order confirmation returned after a successful checkout.
/// </summary>
public class OrderConfirmationResponse
{
    /// <summary>Generated order/tracking number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Grand total charged.</summary>
    public decimal Total { get; set; }

    /// <summary>Order status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC placement timestamp.</summary>
    public DateTime PlacedAt { get; set; }
}
