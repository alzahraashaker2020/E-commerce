namespace Shop.DAL.Models;

/// <summary>
/// A sellable catalog product. May have variants, images, and inventory rows.
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

    /// <summary>Optional colour attribute.</summary>
    public string? Colour { get; set; }

    /// <summary>Optional size attribute.</summary>
    public string? Size { get; set; }

    /// <summary>Optional width in centimetres (cm).</summary>
    public decimal? Width { get; set; }

    /// <summary>Optional height in centimetres (cm).</summary>
    public decimal? Height { get; set; }

    /// <summary>Whether the product is active and visible in the storefront.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC timestamp when the product was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning category navigation.</summary>
    public Category? Category { get; set; }

    /// <summary>Variants of this product (size/color/etc.).</summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    /// <summary>Images attached to this product.</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    /// <summary>Inventory rows for this product (per variant or product-level).</summary>
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
