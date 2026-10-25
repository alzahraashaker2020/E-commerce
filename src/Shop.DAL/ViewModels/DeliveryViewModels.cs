using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>Governorate editor payload.</summary>
public class GovernorateRequest
{
    /// <summary>Display name.</summary>
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    /// <summary>Delivery availability.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Delivery area editor payload.</summary>
public class DeliveryAreaRequest : GovernorateRequest
{
    /// <summary>Owning governorate.</summary>
    [Range(1, int.MaxValue)]
    public int GovernorateId { get; set; }
    /// <summary>Nonnegative shipping charge.</summary>
    [Range(0, 1000000)]
    public decimal ShippingFee { get; set; }
    /// <summary>Configured rate currency.</summary>
    [Required, StringLength(10)]
    public string CurrencyCode { get; set; } = "";
}

/// <summary>Server-calculated checkout summary.</summary>
public class CheckoutQuote
{
    /// <summary>Product subtotal.</summary>
    public decimal Subtotal { get; set; }
    /// <summary>Valid discount amount.</summary>
    public decimal DiscountTotal { get; set; }
    /// <summary>Configured shipping charge.</summary>
    public decimal ShippingTotal { get; set; }
    /// <summary>Final total including shipping.</summary>
    public decimal Total { get; set; }
    /// <summary>Order currency.</summary>
    public string CurrencyCode { get; set; } = "";
}

/// <summary>Minimal data needed to calculate an authoritative checkout quote.</summary>
public class CheckoutQuoteRequest
{
    /// <summary>Requested delivery area.</summary>
    [Range(1, int.MaxValue)]
    public int AreaId { get; set; }
    /// <summary>Cart lines.</summary>
    [Required, MinLength(1)]
    public List<CartLineRequest> Lines { get; set; } = new();
    /// <summary>Optional discount.</summary>
    [StringLength(40)]
    public string? DiscountCode { get; set; }
}
