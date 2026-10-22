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
