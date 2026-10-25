using System.ComponentModel.DataAnnotations;
using Shop.DAL.Models;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Guest checkout request. Totals are computed server-side from trusted prices.
/// </summary>
public class PlaceOrderRequest
{
    /// <summary>Selected delivery area; fee and location names are resolved server-side.</summary>
    [Range(1, int.MaxValue)]
    public int AreaId { get; set; }
    /// <summary>Optional final quote total accepted by the customer.</summary>
    public decimal? ExpectedTotal { get; set; }
    /// <summary>Currency displayed in the accepted quote.</summary>
    [StringLength(10)]
    public string? ExpectedCurrencyCode { get; set; }
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

    /// <summary>Primary shipping address line (required).</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>Optional secondary shipping address line.</summary>
    [StringLength(200)]
    public string? AddressLine2 { get; set; }

    /// <summary>Shipping city (required).</summary>
    [StringLength(120, MinimumLength = 1)]
    public string City { get; set; } = string.Empty;

    /// <summary>Shipping state, region, or governorate (required).</summary>
    [StringLength(120, MinimumLength = 1)]
    public string StateRegion { get; set; } = string.Empty;

    /// <summary>Shipping postal/ZIP code (required).</summary>
    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Shipping country (required).</summary>
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Country { get; set; } = string.Empty;

    /// <summary>Contact phone number for the delivery (required).</summary>
    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>Chosen payment method (Cash or Card).</summary>
    [Required]
    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod PaymentMethod { get; set; }
}

/// <summary>
/// Order confirmation returned after a successful checkout.
/// </summary>
public class OrderConfirmationResponse
{
    /// <summary>Product subtotal.</summary>
    public decimal Subtotal { get; set; }
    /// <summary>Discount charged.</summary>
    public decimal DiscountTotal { get; set; }
    /// <summary>Shipping charge snapshot.</summary>
    public decimal ShippingTotal { get; set; }
    /// <summary>Generated order/tracking number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Grand total charged.</summary>
    public decimal Total { get; set; }

    /// <summary>Order status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC placement timestamp.</summary>
    public DateTime PlacedAt { get; set; }

    /// <summary>ISO currency code the order was priced in.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Chosen payment method (Cash or Card).</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Settlement state of the payment.</summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>Primary shipping address line.</summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>Optional secondary shipping address line.</summary>
    public string? AddressLine2 { get; set; }

    /// <summary>Shipping city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Shipping state, region, or governorate.</summary>
    public string StateRegion { get; set; } = string.Empty;

    /// <summary>Shipping postal/ZIP code.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Shipping country.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Contact phone number for the delivery.</summary>
    public string ContactPhone { get; set; } = string.Empty;
}
