namespace Shop.DAL.ViewModels;

/// <summary>
/// A low-stock inventory row. One entry per inventory row at/below its
/// threshold, so a specific low-stock variant is identifiable.
/// </summary>
public class LowStockItemVM
{
    /// <summary>Product identifier.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Variant identifier, when the row is variant-level.</summary>
    public int? VariantId { get; set; }

    /// <summary>Variant label (option name/value), when applicable.</summary>
    public string? VariantLabel { get; set; }

    /// <summary>Units currently available.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Low-stock threshold.</summary>
    public int LowStockThreshold { get; set; }
}
