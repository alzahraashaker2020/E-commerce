namespace Shop.Application.Catalog;

public static class VariantChangePolicy
{
    public static List<CatalogVariant> BuildVariants(ProductInput request)
    {
        if (!request.HasVariants || request.Variants is null)
        {
            return new List<CatalogVariant>();
        }

        return request.Variants.Select(v => new CatalogVariant
        {
            SizeId = v.SizeId,
            ColorId = v.ColorId,
            Sku = v.Sku,
            Price = v.Price,
            StockQuantity = v.StockQuantity,
            LowStockThreshold = v.LowStockThreshold,
            IsActive = v.IsActive,
            ImageUrl = v.ImageUrls is null ? v.ImageUrl : CleanImages(v.ImageUrls).FirstOrDefault(),
            ImageUrls = v.ImageUrls is null ? new List<string>() : CleanImages(v.ImageUrls),
            IsDemoData = false
        }).ToList();
    }

    public static void SetPrimaryImage(CatalogProduct product, string? imageUrl)
    {
        if (imageUrl is null) return;
        var primary = product.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            if (primary is not null) product.Images.Remove(primary);
        }
        else if (primary is not null) primary.Url = imageUrl;
        else product.Images.Add(new CatalogImage { Url = imageUrl, AltText = product.Name, SortOrder = 0 });
    }

    public static List<string> CleanImages(IEnumerable<string> images) =>
        images.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).Distinct().ToList();

    public static void SetGallery(CatalogProduct product, List<string>? images)
    {
        if (images is null) return;
        product.Images.Clear();
        int position = 0;
        foreach (var url in CleanImages(images))
            product.Images.Add(new CatalogImage { Url = url, AltText = product.Name, SortOrder = position++ });
    }

    /// <summary>
    /// Produces a simple URL-friendly slug from the supplied text.
    /// </summary>
    /// <param name="text">The source text (product name or supplied slug).</param>
    public static string Slugify(string text)
    {
        string lowered = text.Trim().ToLowerInvariant();
        var builder = new System.Text.StringBuilder(lowered.Length);
        bool lastDash = false;

        foreach (char c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastDash = false;
            }
            else if (!lastDash)
            {
                builder.Append('-');
                lastDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

}
