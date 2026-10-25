namespace Shop.DAL.ViewModels;

/// <summary>
/// Compact product representation for catalog list rows.
/// </summary>
public class ProductListItemVM
{
    /// <summary>Product identifier.</summary>
    public int Id { get; set; }

    /// <summary>Product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Brand or manufacturer.</summary>
    public string? Brand { get; set; }

    /// <summary>Base price.</summary>
    public decimal BasePrice { get; set; }

    /// <summary>Owning category identifier.</summary>
    public int CategoryId { get; set; }

    /// <summary>Owning category name.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Whether the product is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Primary (first) image URL, if any.</summary>
    public string? PrimaryImageUrl { get; set; }

    /// <summary>Whether the product sells through variants.</summary>
    public bool HasVariants { get; set; }

    /// <summary>Whether remaining stock is shown on the storefront.</summary>
    public bool ShowStockQuantity { get; set; }

    /// <summary>Product-level units available (admin list; product-level stock).</summary>
    public int StockQuantity { get; set; }

    /// <summary>Product-level low-stock threshold.</summary>
    public int LowStockThreshold { get; set; }
}

/// <summary>
/// Full product representation for the product detail page.
/// </summary>
public class ProductDetailVM
{
    /// <summary>Product identifier.</summary>
    public int Id { get; set; }

    /// <summary>Product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Rich HTML description.</summary>
    public string? DescriptionHtml { get; set; }

    /// <summary>Brand or manufacturer.</summary>
    public string? Brand { get; set; }

    /// <summary>Base price.</summary>
    public decimal BasePrice { get; set; }

    /// <summary>Owning category identifier.</summary>
    public int CategoryId { get; set; }

    /// <summary>Owning category name.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Whether the product is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Variants of the product.</summary>
    public List<ProductVariantVM> Variants { get; set; } = new List<ProductVariantVM>();

    /// <summary>Images of the product.</summary>
    public List<ProductImageVM> Images { get; set; } = new List<ProductImageVM>();

    /// <summary>Whether the product sells through variants.</summary>
    public bool HasVariants { get; set; }

    /// <summary>Whether remaining stock is shown on the storefront.</summary>
    public bool ShowStockQuantity { get; set; }

    /// <summary>Product-level units available (used when the product has no variants).</summary>
    public int StockQuantity { get; set; }

    /// <summary>Product-level low-stock threshold.</summary>
    public int LowStockThreshold { get; set; }
}

/// <summary>
/// Category representation. Carries the hierarchy fields so the storefront can
/// render top-level category cards and drill into subcategories.
/// </summary>
public class CategoryVM
{
    /// <summary>Category identifier.</summary>
    public int Id { get; set; }

    /// <summary>Category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional category card image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Parent category id, or null when this is a top-level category.</summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Child subcategories. Populated when the tree is requested so a single
    /// call can render a top-level category and its subcategories.
    /// </summary>
    public List<CategoryVM> Children { get; set; } = new List<CategoryVM>();
}

/// <summary>
/// Product variant representation (normalized size/color shape).
/// </summary>
public class ProductVariantVM
{
    /// <summary>Ordered variant gallery.</summary>
    public List<string> ImageUrls { get; set; } = new();
    /// <summary>Variant identifier.</summary>
    public int Id { get; set; }

    /// <summary>Size identifier, when the variant has a size.</summary>
    public int? SizeId { get; set; }

    /// <summary>Size display name, when the variant has a size.</summary>
    public string? SizeName { get; set; }

    /// <summary>Color identifier, when the variant has a color.</summary>
    public int? ColorId { get; set; }

    /// <summary>Color display name, when the variant has a color.</summary>
    public string? ColorName { get; set; }

    /// <summary>Optional stock-keeping unit.</summary>
    public string? Sku { get; set; }

    /// <summary>Selling price.</summary>
    public decimal Price { get; set; }

    /// <summary>Units currently available for this variant.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Whether the variant is active and sellable.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional variant-specific image URL.</summary>
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Product image representation.
/// </summary>
public class ProductImageVM
{
    /// <summary>Image identifier.</summary>
    public int Id { get; set; }

    /// <summary>Image URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Accessible alternate text.</summary>
    public string? AltText { get; set; }

    /// <summary>Ordering position.</summary>
    public int SortOrder { get; set; }
}
