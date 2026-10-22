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

    /// <summary>UTC timestamp when the order was placed.</summary>
    public DateTime PlacedAt { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning customer navigation, when the order is not a guest order.</summary>
    public Customer? Customer { get; set; }

    /// <summary>Line items on this order.</summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
