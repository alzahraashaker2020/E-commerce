using System.ComponentModel.DataAnnotations;
using Shop.DAL.Models;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Order summary row for the admin orders list.
/// </summary>
public class MerchantOrderListItemVM
{
    /// <summary>Shipping fee charged at checkout.</summary>
    public decimal ShippingTotal { get; set; }
    /// <summary>Subtotal before shipping and discounts.</summary>
    public decimal Subtotal { get; set; }
    /// <summary>Discount charged.</summary>
    public decimal DiscountTotal { get; set; }
    /// <summary>Configured status identifier.</summary>
    public int? StatusId { get; set; }
    /// <summary>Configured badge color.</summary>
    public string? StatusColor { get; set; }
    /// <summary>Order identifier.</summary>
    public int Id { get; set; }

    /// <summary>Order/tracking number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Customer or guest display name.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Current status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Grand total.</summary>
    public decimal Total { get; set; }

    /// <summary>Chosen payment method (e.g. Cash or Card).</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Settlement state of the payment (e.g. Pending, Paid, CashOnDelivery).</summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>ISO currency code the order was priced in.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Guest email when the order has no customer.</summary>
    public string? GuestEmail { get; set; }

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

    /// <summary>UTC placement timestamp.</summary>
    public DateTime PlacedAt { get; set; }
}

/// <summary>
/// Request to change an order's status.
/// </summary>
public class UpdateOrderStatusRequest
{
    /// <summary>New order status (must be a defined value).</summary>
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus? Status { get; set; }
    /// <summary>Configured target status; preferred over the legacy behavior field.</summary>
    public int? StatusId { get; set; }
}
