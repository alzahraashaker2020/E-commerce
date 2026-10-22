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

    /// <summary>Total units available across inventory rows.</summary>
    public int TotalQuantityOnHand { get; set; }
}

/// <summary>
/// Category representation.
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
}

/// <summary>
/// Product variant representation.
/// </summary>
public class ProductVariantVM
{
    /// <summary>Variant identifier.</summary>
    public int Id { get; set; }

    /// <summary>Stock-keeping unit.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Option dimension name, e.g. "Size".</summary>
    public string OptionName { get; set; } = string.Empty;

    /// <summary>Option value, e.g. "Large".</summary>
    public string OptionValue { get; set; } = string.Empty;

    /// <summary>Selling price.</summary>
    public decimal Price { get; set; }

    /// <summary>Optional compare-at price.</summary>
    public decimal? CompareAtPrice { get; set; }
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
