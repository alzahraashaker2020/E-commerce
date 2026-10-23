using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Admin;

/// <summary>
/// Admin products management: list, read, create, update, delete, and a bulk
/// update endpoint. JWT-protected under the <c>api/admin/products</c> route.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/products")]
public class AdminProductsController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="AdminProductsController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AdminProductsController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Lists all products as detail view models.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProductDetailVM>>>> GetAll()
    {
        List<Product> products = await _repo._Product.Query()
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.InventoryItems)
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
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.InventoryItems)
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
        product.Slug = Slugify(request.Slug ?? request.Name);
        product.CreatedAt = DateTime.UtcNow;
        product.IsDemoData = false;

        _repo._Product.Add(product);
        _repo.Save();

        // With the product Id assigned, upsert its product-level inventory row.
        UpsertProductInventory(product, request);
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
        // Load with inventory so the product-level row upsert can see it.
        Product? product = await _repo._Product.Query()
            .Include(p => p.InventoryItems)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(ApiResponse<ProductDetailVM>.Fail("Not found", 404));
        }

        // Map the editable fields onto the tracked entity; server-owned members
        // are ignored by the write map, so copy them explicitly.
        _mapper.Map(request, product);
        product.Slug = Slugify(request.Slug ?? request.Name);

        _repo._Product.Update(product);

        // Upsert the single product-level inventory row (variant rows untouched).
        UpsertProductInventory(product, request);
        _repo.Save();

        return Ok(ApiResponse<ProductDetailVM>.Ok(await GetDetailVM(product.Id)));
    }

    /// <summary>
    /// Creates or updates the single product-level <see cref="InventoryItem"/>
    /// (the one with <c>VariantId == null</c>) from the request quantity and
    /// threshold. Variant-level inventory rows are left untouched.
    /// </summary>
    /// <param name="product">The product whose inventory is being set.</param>
    /// <param name="request">The upsert payload carrying the quantity fields.</param>
    private void UpsertProductInventory(Product product, ProductUpsertRequest request)
    {
        InventoryItem? item = product.InventoryItems
            .FirstOrDefault(i => i.VariantId == null);

        if (item is null)
        {
            // No product-level row yet: create one.
            _repo._InventoryItem.Add(new InventoryItem
            {
                ProductId = product.Id,
                VariantId = null,
                QuantityOnHand = request.QuantityOnHand,
                LowStockThreshold = request.LowStockThreshold,
                IsDemoData = false
            });
        }
        else
        {
            // Reuse the existing row so no duplicate is created.
            item.QuantityOnHand = request.QuantityOnHand;
            item.LowStockThreshold = request.LowStockThreshold;
            _repo._InventoryItem.Update(item);
        }
    }

    /// <summary>
    /// Re-queries a product with all related data and projects it to a
    /// <see cref="ProductDetailVM"/> so the response reflects the latest
    /// attributes and inventory.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    private async Task<ProductDetailVM> GetDetailVM(int id)
    {
        Product? reloaded = await _repo._Product.Query()
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.InventoryItems)
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
