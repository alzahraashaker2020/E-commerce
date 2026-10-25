namespace Shop.DAL.Models;

/// <summary>
/// A single line within an <see cref="Order"/>. Snapshot fields preserve the
/// product name and price at the time of purchase.
/// </summary>
public class OrderItem
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="Order"/>.</summary>
    public int OrderId { get; set; }

    /// <summary>Foreign key to the purchased <see cref="Product"/>.</summary>
    public int ProductId { get; set; }

    /// <summary>Optional foreign key to a specific <see cref="ProductVariant"/>.</summary>
    public int? VariantId { get; set; }

    /// <summary>Product name captured at purchase time.</summary>
    public string ProductNameSnapshot { get; set; } = string.Empty;

    /// <summary>Size name captured at purchase time, when the variant had a size.</summary>
    public string? SizeSnapshot { get; set; }

    /// <summary>Color name captured at purchase time, when the variant had a color.</summary>
    public string? ColorSnapshot { get; set; }

    /// <summary>Unit price captured at purchase time.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Quantity purchased.</summary>
    public int Quantity { get; set; }

    /// <summary>Line total (<see cref="UnitPrice"/> * <see cref="Quantity"/>).</summary>
    public decimal LineTotal { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning order navigation.</summary>
    public Order? Order { get; set; }

    /// <summary>Purchased product navigation.</summary>
    public Product? Product { get; set; }

    /// <summary>Purchased variant navigation, when set.</summary>
    public ProductVariant? Variant { get; set; }
}
