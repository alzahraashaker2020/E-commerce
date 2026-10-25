namespace Shop.DAL.Models;

/// <summary>
/// A purchasable variant of a <see cref="Product"/>, identified by an optional
/// size and/or color combination. Stock lives on the variant itself
/// (<see cref="StockQuantity"/>) — there is no separate inventory table.
/// </summary>
public class ProductVariant
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="Product"/>.</summary>
    public int ProductId { get; set; }

    /// <summary>Optional foreign key to the <see cref="Models.Size"/> dimension.</summary>
    public int? SizeId { get; set; }

    /// <summary>Optional foreign key to the <see cref="Models.Color"/> dimension.</summary>
    public int? ColorId { get; set; }

    /// <summary>Optional stock-keeping unit identifier.</summary>
    public string? Sku { get; set; }

    /// <summary>Selling price of this variant.</summary>
    public decimal Price { get; set; }

    /// <summary>Units currently available for this variant.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Threshold at/below which this variant is considered low stock.</summary>
    public int LowStockThreshold { get; set; }

    /// <summary>Whether the variant is active and sellable.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional variant-specific image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Ordered gallery; the first image is the primary image.</summary>
    public List<string> ImageUrls { get; set; } = new();

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning product navigation.</summary>
    public Product? Product { get; set; }

    /// <summary>Size navigation, when a size is assigned.</summary>
    public Size? Size { get; set; }

    /// <summary>Color navigation, when a color is assigned.</summary>
    public Color? Color { get; set; }
}
