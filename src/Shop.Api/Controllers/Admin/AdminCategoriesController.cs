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
/// Admin category management for the two-level category tree: list (with parent
/// info and child/product counts), create a top-level category or subcategory,
/// update, and a guarded delete. JWT-protected under the
/// <c>api/admin/categories</c> route.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/categories")]
public class AdminCategoriesController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="AdminCategoriesController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AdminCategoriesController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Lists every category (top-level and subcategories) as a flat list, each
    /// row carrying its parent name and child/product counts.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AdminCategoryListItemVM>>>> GetAll()
    {
        // Load categories with their parent and children so the row can show the
        // parent name and the subcategory count without extra round-trips.
        List<Category> categories = await _repo._Category.Query()
            .Include(c => c.ParentCategory)
            .Include(c => c.Children)
            .OrderBy(c => c.ParentCategoryId ?? 0)
            .ThenBy(c => c.Id)
            .ToListAsync();

        // Per-category product counts, projected in one grouped query.
        Dictionary<int, int> productCounts = await _repo._Product.Query()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        List<AdminCategoryListItemVM> rows = categories.Select(c => new AdminCategoryListItemVM
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            ParentCategoryId = c.ParentCategoryId,
            ParentName = c.ParentCategory?.Name,
            ChildCount = c.Children.Count,
            ProductCount = productCounts.TryGetValue(c.Id, out int count) ? count : 0
        }).ToList();

        return Ok(ApiResponse<List<AdminCategoryListItemVM>>.Ok(rows));
    }

    /// <summary>
    /// Creates a category from the supplied payload. The slug is derived
    /// server-side from the name; a null <c>ParentCategoryId</c> produces a
    /// top-level category, otherwise a subcategory of that parent.
    /// </summary>
    /// <param name="request">The create payload.</param>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminCategoryListItemVM>>> Create(
        [FromBody] CategoryUpsertRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Slug = Slugify(request.Name),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            ParentCategoryId = request.ParentCategoryId,
            IsDemoData = false
        };

        _repo._Category.Add(category);
        _repo.Save();

        return Ok(ApiResponse<AdminCategoryListItemVM>.Ok(await ToRowAsync(category)));
    }

    /// <summary>
    /// Updates an existing category. Returns 404 when the category does not
    /// exist. The slug is re-derived server-side from the name.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="request">The update payload.</param>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdminCategoryListItemVM>>> Update(
        int id,
        [FromBody] CategoryUpsertRequest request)
    {
        Category? category = await _repo._Category.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound(ApiResponse<AdminCategoryListItemVM>.Fail("Not found", 404));
        }

        category.Name = request.Name;
        category.Slug = Slugify(request.Name);
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.ParentCategoryId = request.ParentCategoryId;

        _repo._Category.Update(category);
        _repo.Save();

        return Ok(ApiResponse<AdminCategoryListItemVM>.Ok(await ToRowAsync(category)));
    }

    /// <summary>
    /// Deletes a category by id. Returns 404 when missing, or 409 Conflict with
    /// a clear message when the category still has subcategories or products.
    /// Otherwise deletes and saves.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        Category? category = await _repo._Category.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound(ApiResponse<bool>.Fail("Not found", 404));
        }

        // Guard: a category that still owns subcategories or products cannot be
        // deleted — surface a clear 409 so the UI can explain why.
        bool hasChildren = await _repo._Category.Query().AnyAsync(c => c.ParentCategoryId == id);
        bool hasProducts = await _repo._Product.Query().AnyAsync(p => p.CategoryId == id);
        if (hasChildren || hasProducts)
        {
            return Conflict(ApiResponse<bool>.Fail(
                "Cannot delete a category that has subcategories or products. Remove or reassign them first.",
                409));
        }

        _repo._Category.Delete(category);
        _repo.Save();

        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Builds an <see cref="AdminCategoryListItemVM"/> for a single category,
    /// resolving the parent name and the current child/product counts.
    /// </summary>
    /// <param name="category">The category to project.</param>
    private async Task<AdminCategoryListItemVM> ToRowAsync(Category category)
    {
        string? parentName = category.ParentCategoryId.HasValue
            ? await _repo._Category.Query()
                .Where(c => c.Id == category.ParentCategoryId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync()
            : null;

        int childCount = await _repo._Category.Query().CountAsync(c => c.ParentCategoryId == category.Id);
        int productCount = await _repo._Product.Query().CountAsync(p => p.CategoryId == category.Id);

        return new AdminCategoryListItemVM
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ParentCategoryId = category.ParentCategoryId,
            ParentName = parentName,
            ChildCount = childCount,
            ProductCount = productCount
        };
    }

    /// <summary>
    /// Produces a simple URL-friendly slug from the supplied text (same scheme
    /// as <c>AdminProductsController.Slugify</c>).
    /// </summary>
    /// <param name="text">The source text (category name).</param>
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
