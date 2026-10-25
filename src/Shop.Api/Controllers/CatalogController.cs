using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Public storefront catalog endpoints: paged product listing (with optional
/// category filter) and single-product detail.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="CatalogController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public CatalogController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns a page of products with the total count so the storefront can
    /// restore browse position. An optional <paramref name="categoryId"/>
    /// filters the list; a provided-but-nonexistent category yields an empty
    /// page (TotalCount=0) with an explanatory message and HTTP 200 — the
    /// filter is never silently ignored.
    /// </summary>
    /// <param name="page">1-based page number (clamped to &gt;= 1).</param>
    /// <param name="pageSize">Page size (clamped to 1-100, default 12).</param>
    /// <param name="categoryId">Optional category filter.</param>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultVM<ProductListItemVM>>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] int? categoryId = null)
    {
        // Clamp paging to valid bounds (page >= 1, pageSize 1-100, default 12).
        (int clampedPage, int clampedPageSize) = PagingHelper.Clamp(page, pageSize);

        // A provided categoryId that does not exist returns an EMPTY page at the
        // requested (preserved) position rather than throwing or ignoring the
        // filter, so the client keeps its page/scroll state intact.
        if (categoryId.HasValue && await _repo._Category.GetByIdAsync(categoryId.Value) is null)
        {
            var empty = new PagedResultVM<ProductListItemVM>
            {
                Items = new List<ProductListItemVM>(),
                TotalCount = 0,
                Page = clampedPage,
                PageSize = clampedPageSize
            };

            return Ok(ApiResponse<PagedResultVM<ProductListItemVM>>.Ok(empty, "Category not found"));
        }

        (List<Product> items, int totalCount) =
            await _repo._Product.GetPagedAsync(clampedPage, clampedPageSize, categoryId);

        var result = new PagedResultVM<ProductListItemVM>
        {
            Items = _mapper.Map<List<ProductListItemVM>>(items),
            TotalCount = totalCount,
            Page = clampedPage,
            PageSize = clampedPageSize
        };

        return Ok(ApiResponse<PagedResultVM<ProductListItemVM>>.Ok(result));
    }

    /// <summary>
    /// Returns the full detail for a single product, or a 404 failure envelope
    /// when no product matches <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDetailVM>>> GetProduct(int id)
    {
        // Load the product with its variants, images, and inventory so the
        // detail VM can project variants/images and sum stock on hand.
        Product? product = await _repo._Product.Query()
            .Include(p => p.Category)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            ApiResponse<ProductDetailVM> fail = ApiResponse<ProductDetailVM>.Fail("Product not found", 404);
            return NotFound(fail);
        }

        return Ok(ApiResponse<ProductDetailVM>.Ok(_mapper.Map<ProductDetailVM>(product)));
    }
}
