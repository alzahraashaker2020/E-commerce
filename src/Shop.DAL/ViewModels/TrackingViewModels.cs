namespace Shop.DAL.ViewModels;

/// <summary>
/// Order tracking view returned for a given order number.
/// </summary>
public class OrderTrackingVM
{
    /// <summary>Shipping charge snapshot.</summary>
    public decimal ShippingTotal { get; set; }
    /// <summary>Product subtotal snapshot.</summary>
    public decimal Subtotal { get; set; }
    /// <summary>Discount snapshot.</summary>
    public decimal DiscountTotal { get; set; }
    /// <summary>The currency recorded when the order was placed.</summary>
    public string CurrencyCode { get; set; } = "";
    /// <summary>Order/tracking number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Current status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC placement timestamp.</summary>
    public DateTime PlacedAt { get; set; }

    /// <summary>Line summary.</summary>
    public List<OrderItemVM> Items { get; set; } = new List<OrderItemVM>();

    /// <summary>Grand total.</summary>
    public decimal Total { get; set; }
}

/// <summary>
/// An order line summary.
/// </summary>
public class OrderItemVM
{
    /// <summary>Product name captured at purchase time.</summary>
    public string ProductNameSnapshot { get; set; } = string.Empty;

    /// <summary>Unit price at purchase time.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Quantity purchased.</summary>
    public int Quantity { get; set; }

    /// <summary>Line total.</summary>
    public decimal LineTotal { get; set; }
}
