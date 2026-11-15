using Shop.Application.Common;
namespace Shop.Application.Catalog;

public sealed class ProductManagementService
{
    private readonly IProductCatalogStore _store; private readonly IClock _clock;
    public ProductManagementService(IProductCatalogStore store, IClock clock) { _store = store; _clock = clock; }
    public async Task<ProductOutcome> CreateAsync(ProductInput request, int? limit, CancellationToken ct = default)
    {
        await using var transaction = await _store.BeginCreateAsync(ct);
        if (limit.HasValue && await _store.CountAsync(ct) >= limit.Value) return new(ProductOutcomeKind.Forbidden, Message: "Product limit reached for your subscription plan.");
        var product = new CatalogProduct(); Apply(request, product);
        product.Slug = VariantChangePolicy.Slugify(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug); product.CreatedAt = _clock.UtcNow; product.IsDemoData = false;
        product.Variants = VariantChangePolicy.BuildVariants(request); VariantChangePolicy.SetPrimaryImage(product, request.ImageUrl); VariantChangePolicy.SetGallery(product, request.ImageUrls);
        try { product.Id = _store.Save(product); await transaction.CommitAsync(ct); }
        catch (ProductCapacityException) { return new(ProductOutcomeKind.Conflict, Message: "Product capacity changed concurrently. Refresh and try again."); }
        return new(ProductOutcomeKind.Success, product.Id);
    }
    public async Task<ProductOutcome> UpdateAsync(int id, ProductInput request, CancellationToken ct = default)
    {
        // Keep existing variant identifiers so saved order/cart references remain valid.
        CatalogProduct? product = await _store.LoadAsync(id, ct);
        if (product is null)
        {
            return new(ProductOutcomeKind.NotFound, Message: "Not found");
        }

        // Map the editable fields onto the tracked entity; server-owned members
        // are ignored by the write map, so copy them explicitly.
        Apply(request, product);
        product.Slug = VariantChangePolicy.Slugify(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);

        var rows = request.HasVariants ? request.Variants : new List<VariantInput>();
        if (rows.Where(v => v.Id.HasValue).GroupBy(v => v.Id).Any(g => g.Count() > 1) ||
            rows.Any(v => v.Id.HasValue && !product.Variants.Any(old => old.Id == v.Id)))
            return new(ProductOutcomeKind.Invalid, Message: "Invalid or repeated variant identifier.");
        foreach (var old in product.Variants.ToList())
        {
            // Match dimensions for older clients that did not send identifiers.
            var row = rows.FirstOrDefault(v => v.Id == old.Id ||
                (!v.Id.HasValue && v.SizeId == old.SizeId && v.ColorId == old.ColorId));
            if (row is null)
            {
                if (await _store.HasOrderReferenceAsync(old.Id, ct) ||
                    await _store.HasCartReferenceAsync(old.Id, ct))
                    return new(ProductOutcomeKind.Conflict, Message: "A used variant cannot be removed. Mark it inactive instead.");
                product.Variants.Remove(old);
            }
            else
            {
                if ((old.SizeId != row.SizeId || old.ColorId != row.ColorId) &&
                    await _store.HasOrderReferenceAsync(old.Id, ct))
                    return new(ProductOutcomeKind.Conflict, Message: "The size/color of a sold variant cannot change. Add a new variant instead.");
                row.Id = old.Id;
                old.SizeId = row.SizeId; old.ColorId = row.ColorId; old.Sku = row.Sku;
                old.Price = row.Price; old.StockQuantity = row.StockQuantity;
                old.LowStockThreshold = row.LowStockThreshold; old.IsActive = row.IsActive; old.ImageUrl = row.ImageUrl;
                if (row.ImageUrls is not null)
                {
                    old.ImageUrls = VariantChangePolicy.CleanImages(row.ImageUrls);
                    old.ImageUrl = old.ImageUrls.FirstOrDefault();
                }
            }
        }
        foreach (var row in rows.Where(v => !v.Id.HasValue))
            product.Variants.Add(VariantChangePolicy.BuildVariants(new ProductInput { HasVariants = true, Variants = new() { row } })[0]);
        VariantChangePolicy.SetPrimaryImage(product, request.ImageUrl);
        VariantChangePolicy.SetGallery(product, request.ImageUrls);

        _store.Save(product);

        return new(ProductOutcomeKind.Success, product.Id);
    }
    public async Task<ProductOutcome> DeleteAsync(int id, CancellationToken ct = default) => await _store.DeleteAsync(id, ct) ? new(ProductOutcomeKind.Success) : new(ProductOutcomeKind.NotFound, Message: "Not found");
    public async Task<ProductOutcome> BulkUpdateAsync(BulkProductInput request, CancellationToken ct = default)
    {
        if (request.ProductIds is null || request.ProductIds.Count == 0) return new(ProductOutcomeKind.Invalid, Message: "No products specified");
        var products = await _store.LoadManyAsync(request.ProductIds, ct);
        foreach (var product in products) { if (request.NewPrice.HasValue) product.BasePrice = request.NewPrice.Value; if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value; if (request.CategoryId.HasValue) product.CategoryId = request.CategoryId.Value; }
        _store.SaveMany(products); return new(ProductOutcomeKind.Success, Count: products.Count);
    }
    private static void Apply(ProductInput request, CatalogProduct product)
    {
        product.Name = request.Name;
        product.DescriptionHtml = request.DescriptionHtml;
        product.Brand = request.Brand;
        product.BasePrice = request.BasePrice;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.HasVariants = request.HasVariants;
        product.ShowStockQuantity = request.ShowStockQuantity;
        product.StockQuantity = request.StockQuantity;
        product.LowStockThreshold = request.LowStockThreshold;
    }
}
