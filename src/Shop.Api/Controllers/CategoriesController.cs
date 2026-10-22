using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Public endpoint listing all catalog categories.
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
    /// Returns all categories.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryVM>>>> GetCategories()
    {
        List<Category> categories = await _repo._Category.GetAllAsync();
        List<CategoryVM> vms = _mapper.Map<List<CategoryVM>>(categories);
        return Ok(ApiResponse<List<CategoryVM>>.Ok(vms));
    }
}
