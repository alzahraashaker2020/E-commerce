using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Create/update payload for a product.
/// </summary>
public class ProductUpsertRequest
{
    /// <summary>Product name (required, max 200 chars).</summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    [StringLength(200)]
    public string? Slug { get; set; }

    /// <summary>Rich HTML description.</summary>
    public string? DescriptionHtml { get; set; }

    /// <summary>Brand or manufacturer.</summary>
    [StringLength(120)]
    public string? Brand { get; set; }

    /// <summary>Base price (must be &gt;= 0).</summary>
    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }

    /// <summary>Owning category identifier.</summary>
    [Required]
    public int CategoryId { get; set; }

    /// <summary>Whether the product is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Whether the product sells through variants.</summary>
    public bool HasVariants { get; set; }

    /// <summary>Whether remaining stock is shown on the storefront.</summary>
    public bool ShowStockQuantity { get; set; }

    /// <summary>Product-level units available (used when the product has no variants).</summary>
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    /// <summary>Product-level low-stock threshold.</summary>
    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; }

    /// <summary>
    /// The specific variant rows to persist when <see cref="HasVariants"/> is
    /// true. On update these replace the product's existing variants entirely.
    /// </summary>
    public List<ProductVariantUpsert> Variants { get; set; } = new List<ProductVariantUpsert>();
}

/// <summary>
/// A single variant row within a product create/update payload. Each row is one
/// chosen size/color combination (there is no auto size x color matrix).
/// </summary>
public class ProductVariantUpsert
{
    /// <summary>Optional size identifier for this variant.</summary>
    public int? SizeId { get; set; }

    /// <summary>Optional color identifier for this variant.</summary>
    public int? ColorId { get; set; }

    /// <summary>Optional stock-keeping unit.</summary>
    [StringLength(100)]
    public string? Sku { get; set; }

    /// <summary>Selling price of this variant (must be &gt;= 0).</summary>
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>Units currently available for this variant.</summary>
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    /// <summary>Threshold at/below which this variant is low stock.</summary>
    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; }

    /// <summary>Whether the variant is active and sellable.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional variant-specific image URL.</summary>
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Bulk product update payload. At least one change field must be non-null.
/// </summary>
public class BulkProductUpdateRequest
{
    /// <summary>Products to update (at least one).</summary>
    [Required]
    [MinLength(1)]
    public List<int> ProductIds { get; set; } = new List<int>();

    /// <summary>New base price, when changing price.</summary>
    public decimal? NewPrice { get; set; }

    /// <summary>New active flag, when changing activation.</summary>
    public bool? IsActive { get; set; }

    /// <summary>New category, when recategorizing.</summary>
    public int? CategoryId { get; set; }
}
