using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;
using Shop.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Shop.Api.Controllers.Admin;

/// <summary>
/// Admin management of the store currency settings. JWT-protected under the
/// <c>api/admin/settings</c> route.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/settings")]
public class AdminSettingsController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;
    private readonly ShopContext _db;

    /// <summary>
    /// Creates a new <see cref="AdminSettingsController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AdminSettingsController(IRepoWrapper repo, IMapper mapper, ShopContext db)
    {
        _repo = repo;
        _mapper = mapper;
        _db = db;
    }

    /// <summary>
    /// Returns the current store currency settings (row 1).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<StoreSettingsVM>>> Get()
    {
        StoreSettings? settings = await _repo._StoreSettings.GetByIdAsync(1);
        if (settings is null)
        {
            return NotFound(ApiResponse<StoreSettingsVM>.Fail("Settings not found", 404));
        }

        return Ok(ApiResponse<StoreSettingsVM>.Ok(_mapper.Map<StoreSettingsVM>(settings)));
    }

    /// <summary>
    /// Updates the store currency settings (row 1). Returns 404 when the row
    /// has not been seeded.
    /// </summary>
    /// <param name="request">The currency update payload.</param>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<StoreSettingsVM>>> Update(
        [FromBody] StoreSettingsUpdateRequest request)
    {
        StoreSettings? settings = await _repo._StoreSettings.GetByIdAsync(1);
        if (settings is null)
        {
            return NotFound(ApiResponse<StoreSettingsVM>.Fail("Settings not found", 404));
        }

        var currency = await _db.Currencies.FirstOrDefaultAsync(c => c.Code == request.CurrencyCode.ToUpperInvariant() && c.IsActive);
        if (currency is null) return BadRequest(ApiResponse<StoreSettingsVM>.Fail("Select an active currency from currency management.", 400));
        settings.CurrencyCode = currency.Code;
        settings.CurrencySymbol = currency.Symbol;

        _repo._StoreSettings.Update(settings);
        _repo.Save();

        return Ok(ApiResponse<StoreSettingsVM>.Ok(_mapper.Map<StoreSettingsVM>(settings)));
    }
}
