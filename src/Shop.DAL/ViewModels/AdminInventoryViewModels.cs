namespace Shop.DAL.ViewModels;

/// <summary>
/// A low-stock row. One entry per product (no variants) or per variant that is
/// at/below its low-stock threshold, so a specific low-stock unit is
/// identifiable.
/// </summary>
public class LowStockItemVM
{
    /// <summary>Product identifier.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Variant identifier, when the row is variant-level.</summary>
    public int? VariantId { get; set; }

    /// <summary>Variant label (size / color), when applicable.</summary>
    public string? VariantLabel { get; set; }

    /// <summary>Units currently available.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Low-stock threshold.</summary>
    public int LowStockThreshold { get; set; }
}
