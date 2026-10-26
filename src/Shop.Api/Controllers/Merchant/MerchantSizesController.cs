using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Merchant;

/// <summary>
/// Merchant size management: list, read, create, update, and a reference-safe
/// delete. A size that is still referenced by any <see cref="ProductVariant"/>
/// cannot be deleted (returns 409). JWT-protected under the
/// <c>api/merchant/sizes</c> route.
/// </summary>
[ApiController]
[Authorize(Roles = "Merchant")]
[Route("api/merchant/sizes")]
public class MerchantSizesController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="MerchantSizesController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public MerchantSizesController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Lists every size ordered by display order then name.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SizeVM>>>> GetAll()
    {
        List<Size> sizes = await _repo._Size.Query()
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return Ok(ApiResponse<List<SizeVM>>.Ok(_mapper.Map<List<SizeVM>>(sizes)));
    }

    /// <summary>
    /// Returns a single size by id, or a 404 failure envelope when missing.
    /// </summary>
    /// <param name="id">The size identifier.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SizeVM>>> GetById(int id)
    {
        Size? size = await _repo._Size.GetByIdAsync(id);
        if (size is null)
        {
            return NotFound(ApiResponse<SizeVM>.Fail("Not found", 404));
        }

        return Ok(ApiResponse<SizeVM>.Ok(_mapper.Map<SizeVM>(size)));
    }

    /// <summary>
    /// Creates a size from the supplied payload.
    /// </summary>
    /// <param name="request">The create payload.</param>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SizeVM>>> Create(
        [FromBody] SizeUpsertRequest request)
    {
        var size = new Size
        {
            Name = request.Name,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            IsDemoData = false
        };

        _repo._Size.Add(size);
        _repo.Save();

        return Ok(ApiResponse<SizeVM>.Ok(_mapper.Map<SizeVM>(size)));
    }

    /// <summary>
    /// Updates an existing size. Returns 404 when the size does not exist.
    /// </summary>
    /// <param name="id">The size identifier.</param>
    /// <param name="request">The update payload.</param>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<SizeVM>>> Update(
        int id,
        [FromBody] SizeUpsertRequest request)
    {
        Size? size = await _repo._Size.GetByIdAsync(id);
        if (size is null)
        {
            return NotFound(ApiResponse<SizeVM>.Fail("Not found", 404));
        }

        size.Name = request.Name;
        size.DisplayOrder = request.DisplayOrder;
        size.IsActive = request.IsActive;

        _repo._Size.Update(size);
        _repo.Save();

        return Ok(ApiResponse<SizeVM>.Ok(_mapper.Map<SizeVM>(size)));
    }

    /// <summary>
    /// Deletes a size by id. Returns 404 when missing, or 409 Conflict with a
    /// clear message when any product variant still references it. Otherwise
    /// deletes and saves.
    /// </summary>
    /// <param name="id">The size identifier.</param>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        Size? size = await _repo._Size.GetByIdAsync(id);
        if (size is null)
        {
            return NotFound(ApiResponse<bool>.Fail("Not found", 404));
        }

        // Guard: a size still used by a variant cannot be deleted — surface a
        // clear 409 so the UI can explain why.
        bool referenced = await _repo._Product.Query()
            .AnyAsync(p => p.Variants.Any(v => v.SizeId == id));
        if (referenced)
        {
            return Conflict(ApiResponse<bool>.Fail(
                "Cannot delete a size that is still used by one or more product variants. Reassign or remove those variants first.",
                409));
        }

        _repo._Size.Delete(size);
        _repo.Save();

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
