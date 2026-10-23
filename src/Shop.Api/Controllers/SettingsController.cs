using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Public endpoint exposing the store-wide settings (currency) so the
/// storefront can format prices in the configured currency.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="SettingsController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public SettingsController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns the store currency settings (the single row, Id = 1). Falls back
    /// to USD/$ when no row has been seeded yet.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<StoreSettingsVM>>> Get()
    {
        StoreSettings? settings = await _repo._StoreSettings.GetByIdAsync(1);
        StoreSettingsVM vm = settings is not null
            ? _mapper.Map<StoreSettingsVM>(settings)
            : new StoreSettingsVM { CurrencyCode = "USD", CurrencySymbol = "$" };

        return Ok(ApiResponse<StoreSettingsVM>.Ok(vm));
    }
}
