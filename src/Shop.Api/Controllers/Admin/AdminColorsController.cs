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
/// Admin color management: list, read, create, update, and a reference-safe
/// delete. A color that is still referenced by any <see cref="ProductVariant"/>
/// cannot be deleted (returns 409). JWT-protected under the
/// <c>api/admin/colors</c> route.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/colors")]
public class AdminColorsController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="AdminColorsController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AdminColorsController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Lists every color ordered by display order then name.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ColorVM>>>> GetAll()
    {
        List<Color> colors = await _repo._Color.Query()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Ok(ApiResponse<List<ColorVM>>.Ok(_mapper.Map<List<ColorVM>>(colors)));
    }

    /// <summary>
    /// Returns a single color by id, or a 404 failure envelope when missing.
    /// </summary>
    /// <param name="id">The color identifier.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ColorVM>>> GetById(int id)
    {
        Color? color = await _repo._Color.GetByIdAsync(id);
        if (color is null)
        {
            return NotFound(ApiResponse<ColorVM>.Fail("Not found", 404));
        }

        return Ok(ApiResponse<ColorVM>.Ok(_mapper.Map<ColorVM>(color)));
    }

    /// <summary>
    /// Creates a color from the supplied payload.
    /// </summary>
    /// <param name="request">The create payload.</param>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ColorVM>>> Create(
        [FromBody] ColorUpsertRequest request)
    {
        var color = new Color
        {
            Name = request.Name,
            HexCode = request.HexCode,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            IsDemoData = false
        };

        _repo._Color.Add(color);
        _repo.Save();

        return Ok(ApiResponse<ColorVM>.Ok(_mapper.Map<ColorVM>(color)));
    }

    /// <summary>
    /// Updates an existing color. Returns 404 when the color does not exist.
    /// </summary>
    /// <param name="id">The color identifier.</param>
    /// <param name="request">The update payload.</param>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ColorVM>>> Update(
        int id,
        [FromBody] ColorUpsertRequest request)
    {
        Color? color = await _repo._Color.GetByIdAsync(id);
        if (color is null)
        {
            return NotFound(ApiResponse<ColorVM>.Fail("Not found", 404));
        }

        color.Name = request.Name;
        color.HexCode = request.HexCode;
        color.DisplayOrder = request.DisplayOrder;
        color.IsActive = request.IsActive;

        _repo._Color.Update(color);
        _repo.Save();

        return Ok(ApiResponse<ColorVM>.Ok(_mapper.Map<ColorVM>(color)));
    }

    /// <summary>
    /// Deletes a color by id. Returns 404 when missing, or 409 Conflict with a
    /// clear message when any product variant still references it. Otherwise
    /// deletes and saves.
    /// </summary>
    /// <param name="id">The color identifier.</param>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        Color? color = await _repo._Color.GetByIdAsync(id);
        if (color is null)
        {
            return NotFound(ApiResponse<bool>.Fail("Not found", 404));
        }

        // Guard: a color still used by a variant cannot be deleted — surface a
        // clear 409 so the UI can explain why.
        bool referenced = await _repo._Product.Query()
            .AnyAsync(p => p.Variants.Any(v => v.ColorId == id));
        if (referenced)
        {
            return Conflict(ApiResponse<bool>.Fail(
                "Cannot delete a color that is still used by one or more product variants. Reassign or remove those variants first.",
                409));
        }

        _repo._Color.Delete(color);
        _repo.Save();

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
