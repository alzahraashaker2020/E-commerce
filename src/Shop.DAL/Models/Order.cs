namespace Shop.DAL.Models;

/// <summary>
/// A placed order. Guest-friendly: <see cref="CustomerId"/> is nullable and
/// guest contact details are captured directly. All money totals are owned and
/// computed by the checkout/business layer, never trusted from the client.
/// </summary>
public class Order
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Human-readable, unique order/tracking number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Optional foreign key to a <see cref="Customer"/> (null for guests).</summary>
    public int? CustomerId { get; set; }

    /// <summary>Guest email when the order has no customer.</summary>
    public string? GuestEmail { get; set; }

    /// <summary>Guest name when the order has no customer.</summary>
    public string? GuestName { get; set; }

    /// <summary>Current lifecycle status.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Sum of line totals before discount/tax/shipping.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Total discount applied.</summary>
    public decimal DiscountTotal { get; set; }

    /// <summary>Tax amount.</summary>
    public decimal Tax { get; set; }

    /// <summary>Shipping charge.</summary>
    public decimal ShippingTotal { get; set; }

    /// <summary>Grand total charged.</summary>
    public decimal Total { get; set; }

    /// <summary>Chosen payment method (cash or card).</summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>Settlement state of the payment.</summary>
    public PaymentStatus PaymentStatus { get; set; }

    /// <summary>ISO currency code the order was priced in (defaults to USD).</summary>
    public string CurrencyCode { get; set; } = "USD";

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

    /// <summary>UTC timestamp when the order was placed.</summary>
    public DateTime PlacedAt { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning customer navigation, when the order is not a guest order.</summary>
    public Customer? Customer { get; set; }

    /// <summary>Line items on this order.</summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
