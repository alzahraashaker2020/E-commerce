namespace Shop.DAL.Models;

/// <summary>
/// Stock record for a sellable unit. Tracked per variant when a product has
/// variants (<see cref="VariantId"/> set), otherwise one row per product.
/// Low-stock is the per-row rule <c>QuantityOnHand &lt;= LowStockThreshold</c>.
/// </summary>
public class InventoryItem
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="Product"/>.</summary>
    public int ProductId { get; set; }

    /// <summary>Optional foreign key to a specific <see cref="ProductVariant"/>.</summary>
    public int? VariantId { get; set; }

    /// <summary>Units currently available.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Threshold at/below which the row is considered low stock.</summary>
    public int LowStockThreshold { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning product navigation.</summary>
    public Product? Product { get; set; }

    /// <summary>Owning variant navigation, when this row is variant-level.</summary>
    public ProductVariant? Variant { get; set; }
}
