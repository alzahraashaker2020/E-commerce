using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Public endpoints for the catalog category tree. Categories form a two-level
/// hierarchy (top-level category → subcategories); these endpoints expose the
/// flat list, the top-level tree (for the home category cards), and the
/// subcategories of a single parent.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="CategoriesController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public CategoriesController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns all categories as a flat list (both top-level and subcategories).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryVM>>>> GetCategories()
    {
        List<Category> categories = await _repo._Category.GetAllAsync();
        List<CategoryVM> vms = _mapper.Map<List<CategoryVM>>(categories);
        return Ok(ApiResponse<List<CategoryVM>>.Ok(vms));
    }

    /// <summary>
    /// Returns the top-level categories, each with its subcategories populated.
    /// Drives the four home-page category cards and their drill-down pages.
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<List<CategoryVM>>>> GetTree()
    {
        // Load top-level categories together with their children in one query.
        List<Category> roots = await _repo._Category.Query()
            .Where(c => c.ParentCategoryId == null)
            .Include(c => c.Children)
            .OrderBy(c => c.Id)
            .ToListAsync();

        List<CategoryVM> vms = _mapper.Map<List<CategoryVM>>(roots);
        return Ok(ApiResponse<List<CategoryVM>>.Ok(vms));
    }

    /// <summary>
    /// Returns a single top-level category by slug with its subcategories, or a
    /// 404 failure envelope when the slug is unknown. Used by the subcategory
    /// listing page (e.g. "Sofa Styling" → its four subcategories).
    /// </summary>
    /// <param name="slug">The top-level category slug.</param>
    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<CategoryVM>>> GetBySlug(string slug)
    {
        Category? category = await _repo._Category.Query()
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Slug == slug);

        if (category is null)
        {
            return NotFound(ApiResponse<CategoryVM>.Fail("Category not found", 404));
        }

        return Ok(ApiResponse<CategoryVM>.Ok(_mapper.Map<CategoryVM>(category)));
    }
}
