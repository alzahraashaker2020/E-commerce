using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.Data;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Admin;

/// <summary>
/// Admin products management: list, read, create, update, delete, and a bulk
/// update endpoint. JWT-protected under the <c>api/admin/products</c> route.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/products")]
public class AdminProductsController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;
    private readonly ShopContext _db;

    /// <summary>
    /// Creates a new <see cref="AdminProductsController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AdminProductsController(IRepoWrapper repo, IMapper mapper, ShopContext db)
    {
        _repo = repo;
        _mapper = mapper;
        _db = db;
    }

    /// <summary>
    /// Lists all products as detail view models.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProductDetailVM>>>> GetAll()
    {
        List<Product> products = await _repo._Product.Query()
            .Include(p => p.Category)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Images)
            .ToListAsync();

        return Ok(ApiResponse<List<ProductDetailVM>>.Ok(_mapper.Map<List<ProductDetailVM>>(products)));
    }

    /// <summary>
    /// Returns a single product by id, or a 404 failure envelope when missing.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDetailVM>>> GetById(int id)
    {
        Product? product = await _repo._Product.Query()
            .Include(p => p.Category)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(ApiResponse<ProductDetailVM>.Fail("Not found", 404));
        }

        return Ok(ApiResponse<ProductDetailVM>.Ok(_mapper.Map<ProductDetailVM>(product)));
    }

    /// <summary>
    /// Creates a product from the supplied payload. The slug is derived
    /// server-side from the name (or the supplied slug), and server-owned
    /// fields are set here rather than trusted from the client.
    /// </summary>
    /// <param name="request">The create payload.</param>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDetailVM>>> Create(
        [FromBody] ProductUpsertRequest request)
    {
        Product product = _mapper.Map<Product>(request);
        product.Slug = Slugify(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);
        product.CreatedAt = DateTime.UtcNow;
        product.IsDemoData = false;

        // Persist the chosen variant rows (only when the product has variants);
        // they are added as part of the same Save() as the product.
        product.Variants = BuildVariants(request);
        SetPrimaryImage(product, request.ImageUrl);
        SetGallery(product, request.ImageUrls);

        _repo._Product.Add(product);
        _repo.Save();

        return Ok(ApiResponse<ProductDetailVM>.Ok(await GetDetailVM(product.Id)));
    }

    /// <summary>
    /// Updates an existing product. Returns 404 when the product does not
    /// exist. The slug is re-derived server-side.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="request">The update payload.</param>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDetailVM>>> Update(
        int id,
        [FromBody] ProductUpsertRequest request)
    {
        // Keep existing variant identifiers so saved order/cart references remain valid.
        Product? product = await _repo._Product.Query()
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(ApiResponse<ProductDetailVM>.Fail("Not found", 404));
        }

        // Map the editable fields onto the tracked entity; server-owned members
        // are ignored by the write map, so copy them explicitly.
        _mapper.Map(request, product);
        product.Slug = Slugify(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);

        var rows = request.HasVariants ? request.Variants : new List<ProductVariantUpsert>();
        if (rows.Where(v => v.Id.HasValue).GroupBy(v => v.Id).Any(g => g.Count() > 1) ||
            rows.Any(v => v.Id.HasValue && !product.Variants.Any(old => old.Id == v.Id)))
            return BadRequest(ApiResponse<ProductDetailVM>.Fail("Invalid or repeated variant identifier.", 400));
        foreach (var old in product.Variants.ToList())
        {
            // Match dimensions for older clients that did not send identifiers.
            var row = rows.FirstOrDefault(v => v.Id == old.Id ||
                (!v.Id.HasValue && v.SizeId == old.SizeId && v.ColorId == old.ColorId));
            if (row is null)
            {
                if (await _db.OrderItems.AnyAsync(i => i.VariantId == old.Id) ||
                    await _db.CartItems.AnyAsync(i => i.VariantId == old.Id))
                    return Conflict(ApiResponse<ProductDetailVM>.Fail("A used variant cannot be removed. Mark it inactive instead.", 409));
                product.Variants.Remove(old);
            }
            else
            {
                if ((old.SizeId != row.SizeId || old.ColorId != row.ColorId) &&
                    await _db.OrderItems.AnyAsync(i => i.VariantId == old.Id))
                    return Conflict(ApiResponse<ProductDetailVM>.Fail("The size/color of a sold variant cannot change. Add a new variant instead.", 409));
                row.Id = old.Id;
                old.SizeId = row.SizeId; old.ColorId = row.ColorId; old.Sku = row.Sku;
                old.Price = row.Price; old.StockQuantity = row.StockQuantity;
                old.LowStockThreshold = row.LowStockThreshold; old.IsActive = row.IsActive; old.ImageUrl = row.ImageUrl;
                if (row.ImageUrls is not null)
                {
                    old.ImageUrls = CleanImages(row.ImageUrls);
                    old.ImageUrl = old.ImageUrls.FirstOrDefault();
                }
            }
        }
        foreach (var row in rows.Where(v => !v.Id.HasValue))
            product.Variants.Add(BuildVariants(new ProductUpsertRequest { HasVariants = true, Variants = new() { row } })[0]);
        SetPrimaryImage(product, request.ImageUrl);
        SetGallery(product, request.ImageUrls);

        _repo._Product.Update(product);
        _repo.Save();

        return Ok(ApiResponse<ProductDetailVM>.Ok(await GetDetailVM(product.Id)));
    }

    /// <summary>
    /// Re-queries a product with all related data and projects it to a
    /// <see cref="ProductDetailVM"/> so the response reflects the latest
    /// attributes and variants.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    private async Task<ProductDetailVM> GetDetailVM(int id)
    {
        Product? reloaded = await _repo._Product.Query()
            .Include(p => p.Category)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        return _mapper.Map<ProductDetailVM>(reloaded);
    }

    /// <summary>
    /// Deletes a product by id using load-then-delete: load the row, return 404
    /// when missing, otherwise delete and save.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        Product? product = await _repo._Product.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound(ApiResponse<bool>.Fail("Not found", 404));
        }

        _repo._Product.Delete(product);
        _repo.Save();

        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Applies a partial bulk change (price/active/category) to the specified
    /// products. Loads them in one query via <c>GetByIdsAsync</c>, applies the
    /// non-null change fields, saves once, and returns the number updated.
    /// </summary>
    /// <param name="request">The bulk update payload.</param>
    [HttpPut("bulk")]
    public async Task<ActionResult<ApiResponse<int>>> BulkUpdate(
        [FromBody] BulkProductUpdateRequest request)
    {
        if (request.ProductIds is null || request.ProductIds.Count == 0)
        {
            return BadRequest(ApiResponse<int>.Fail("No products specified", 400));
        }

        // Bulk-load the target products, apply the partial changes in memory,
        // then commit all of them with a single Save().
        List<Product> products = await _repo._Product.GetByIdsAsync(request.ProductIds);
        foreach (Product product in products)
        {
            if (request.NewPrice.HasValue)
            {
                product.BasePrice = request.NewPrice.Value;
            }

            if (request.IsActive.HasValue)
            {
                product.IsActive = request.IsActive.Value;
            }

            if (request.CategoryId.HasValue)
            {
                product.CategoryId = request.CategoryId.Value;
            }

            _repo._Product.Update(product);
        }

        _repo.Save();

        return Ok(ApiResponse<int>.Ok(products.Count));
    }

    /// <summary>
    /// Builds the variant rows to persist for a product. When the product has
    /// no variants the list is empty; otherwise each submitted row maps to a
    /// new <see cref="ProductVariant"/> (one chosen size/color combination).
    /// </summary>
    /// <param name="request">The product create/update payload.</param>
    private static List<ProductVariant> BuildVariants(ProductUpsertRequest request)
    {
        if (!request.HasVariants || request.Variants is null)
        {
            return new List<ProductVariant>();
        }

        return request.Variants.Select(v => new ProductVariant
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

    private static void SetPrimaryImage(Product product, string? imageUrl)
    {
        if (imageUrl is null) return;
        var primary = product.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            if (primary is not null) product.Images.Remove(primary);
        }
        else if (primary is not null) primary.Url = imageUrl;
        else product.Images.Add(new ProductImage { Url = imageUrl, AltText = product.Name, SortOrder = 0 });
    }

    private static List<string> CleanImages(IEnumerable<string> images) =>
        images.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).Distinct().ToList();

    private static void SetGallery(Product product, List<string>? images)
    {
        if (images is null) return;
        product.Images.Clear();
        int position = 0;
        foreach (var url in CleanImages(images))
            product.Images.Add(new ProductImage { Url = url, AltText = product.Name, SortOrder = position++ });
    }

    /// <summary>
    /// Produces a simple URL-friendly slug from the supplied text.
    /// </summary>
    /// <param name="text">The source text (product name or supplied slug).</param>
    private static string Slugify(string text)
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
