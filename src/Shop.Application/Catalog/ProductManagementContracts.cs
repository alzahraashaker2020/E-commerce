namespace Shop.Application.Catalog;

public class ProductInput
{
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? DescriptionHtml { get; set; }

    public string? Brand { get; set; }

    public decimal BasePrice { get; set; }

    public int CategoryId { get; set; }

    public bool IsActive { get; set; }

    public bool HasVariants { get; set; }

    public bool ShowStockQuantity { get; set; }

    public int StockQuantity { get; set; }

    public int LowStockThreshold { get; set; }

    public string? ImageUrl { get; set; }

    public List<string>? ImageUrls { get; set; }

    public List<VariantInput> Variants { get; set; } = new List<VariantInput>();
}
public class VariantInput
{
    public List<string>? ImageUrls { get; set; }
    public int? Id { get; set; }
    public int? SizeId { get; set; }

    public int? ColorId { get; set; }

    public string? Sku { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public int LowStockThreshold { get; set; }

    public bool IsActive { get; set; }

    public string? ImageUrl { get; set; }
}
public class BulkProductInput
{
    public List<int> ProductIds { get; set; } = new List<int>();

    public decimal? NewPrice { get; set; }

    public bool? IsActive { get; set; }

    public int? CategoryId { get; set; }
}
public sealed class CatalogProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? DescriptionHtml { get; set; }
    public string? Brand { get; set; }
    public decimal BasePrice { get; set; }
    public int CategoryId { get; set; }
    public bool HasVariants { get; set; }
    public bool ShowStockQuantity { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDemoData { get; set; }
    public List<CatalogVariant> Variants { get; set; } = new(); public List<CatalogImage> Images { get; set; } = new();
}
public sealed class CatalogVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? SizeId { get; set; }
    public int? ColorId { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public bool IsDemoData { get; set; }
}
public sealed class CatalogImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsDemoData { get; set; }
}
public enum ProductOutcomeKind { Success, Invalid, NotFound, Forbidden, Conflict }
public sealed record ProductOutcome(ProductOutcomeKind Kind, int Id = 0, int Count = 0, string? Message = null);
public sealed class ProductCapacityException : Exception { }
