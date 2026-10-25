namespace Shop.DAL.Models;

/// <summary>
/// A sellable catalog product. May have variants and images. Stock is tracked
/// on the product itself (<see cref="StockQuantity"/>) when it has no variants,
/// otherwise on each <see cref="ProductVariant"/>.
/// </summary>
public class Product
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name of the product.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly unique slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Rich HTML description for the product detail page.</summary>
    public string? DescriptionHtml { get; set; }

    /// <summary>Brand or manufacturer name.</summary>
    public string? Brand { get; set; }

    /// <summary>Base price used when a product has no variant-level price.</summary>
    public decimal BasePrice { get; set; }

    /// <summary>Foreign key to the owning <see cref="Category"/>.</summary>
    public int CategoryId { get; set; }

    /// <summary>Whether this product sells through <see cref="ProductVariant"/> rows.</summary>
    public bool HasVariants { get; set; }

    /// <summary>Whether the remaining stock quantity is shown on the storefront.</summary>
    public bool ShowStockQuantity { get; set; }

    /// <summary>Product-level units available (used when <see cref="HasVariants"/> is false).</summary>
    public int StockQuantity { get; set; }

    /// <summary>Product-level low-stock threshold (used when <see cref="HasVariants"/> is false).</summary>
    public int LowStockThreshold { get; set; }

    /// <summary>Whether the product is active and visible in the storefront.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC timestamp when the product was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning category navigation.</summary>
    public Category? Category { get; set; }

    /// <summary>Variants of this product (size/color combinations).</summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    /// <summary>Images attached to this product.</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
