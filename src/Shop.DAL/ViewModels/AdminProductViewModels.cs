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

    /// <summary>Optional colour attribute.</summary>
    [StringLength(80)]
    public string? Colour { get; set; }

    /// <summary>Optional size attribute.</summary>
    [StringLength(80)]
    public string? Size { get; set; }

    /// <summary>Optional width in centimetres (cm).</summary>
    [Range(0, double.MaxValue)]
    public decimal? Width { get; set; }

    /// <summary>Optional height in centimetres (cm).</summary>
    [Range(0, double.MaxValue)]
    public decimal? Height { get; set; }

    /// <summary>Units available for the product-level inventory row.</summary>
    [Range(0, int.MaxValue)]
    public int QuantityOnHand { get; set; }

    /// <summary>Low-stock threshold for the product-level inventory row.</summary>
    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; }
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
