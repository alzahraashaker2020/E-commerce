using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.Data;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Merchant;

/// <summary>
/// Merchant products management: list, read, create, update, delete, and a bulk
/// update endpoint. JWT-protected under the <c>api/merchant/products</c> route.
/// </summary>
[ApiController]
[Authorize(Roles = "Merchant")]
[Route("api/merchant/products")]
public class MerchantProductsController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;
    private readonly Shop.Application.Catalog.ProductManagementService _products;

    /// <summary>
    /// Creates a new <see cref="MerchantProductsController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public MerchantProductsController(IRepoWrapper repo, IMapper mapper, Shop.Application.Catalog.ProductManagementService products)
    {
        _repo = repo;
        _mapper = mapper;
        _products = products;
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
        var outcome = await _products.CreateAsync(Shop.Api.Adapters.CheckoutBoundary.Copy<Shop.Application.Catalog.ProductInput>(request), HttpContext.RequestServices.GetRequiredService<Shop.Api.Tenancy.StoreScope>().Subscription?.MaxProducts, HttpContext.RequestAborted);
        if (outcome.Kind != Shop.Application.Catalog.ProductOutcomeKind.Success) return Failure<ProductDetailVM>(outcome);
        return Ok(ApiResponse<ProductDetailVM>.Ok(await GetDetailVM(outcome.Id)));
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
        var outcome = await _products.UpdateAsync(id, Shop.Api.Adapters.CheckoutBoundary.Copy<Shop.Application.Catalog.ProductInput>(request), HttpContext.RequestAborted);
        if (outcome.Kind != Shop.Application.Catalog.ProductOutcomeKind.Success) return Failure<ProductDetailVM>(outcome);
        return Ok(ApiResponse<ProductDetailVM>.Ok(await GetDetailVM(outcome.Id)));
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
        var outcome = await _products.DeleteAsync(id, HttpContext.RequestAborted);
        return outcome.Kind == Shop.Application.Catalog.ProductOutcomeKind.Success ? Ok(ApiResponse<bool>.Ok(true)) : Failure<bool>(outcome);
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
        var outcome = await _products.BulkUpdateAsync(Shop.Api.Adapters.CheckoutBoundary.Copy<Shop.Application.Catalog.BulkProductInput>(request), HttpContext.RequestAborted);
        return outcome.Kind == Shop.Application.Catalog.ProductOutcomeKind.Success ? Ok(ApiResponse<int>.Ok(outcome.Count)) : Failure<int>(outcome);
    }

    /// <summary>
    /// Builds the variant rows to persist for a product. When the product has
    /// no variants the list is empty; otherwise each submitted row maps to a
    /// new <see cref="ProductVariant"/> (one chosen size/color combination).
    /// </summary>
    /// <param name="request">The product create/update payload.</param>
    private static ActionResult<ApiResponse<T>> Failure<T>(Shop.Application.Catalog.ProductOutcome result)
    {
        int status = result.Kind switch { Shop.Application.Catalog.ProductOutcomeKind.Invalid => 400, Shop.Application.Catalog.ProductOutcomeKind.NotFound => 404, Shop.Application.Catalog.ProductOutcomeKind.Forbidden => 403, _ => 409 };
        return new ObjectResult(ApiResponse<T>.Fail(result.Message!, status)) { StatusCode = status };
    }
}
