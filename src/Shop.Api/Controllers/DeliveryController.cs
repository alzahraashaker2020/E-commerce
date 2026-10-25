using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.Api.Controllers;

[ApiController, Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly ShopContext _db;
    public DeliveryController(ShopContext db) => _db = db;
    [HttpGet("governorates")]
    public async Task<IActionResult> Governorates() => Ok(ApiResponse<List<Governorate>>.Ok(await _db.Governorates.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync()));
    [HttpGet("areas")]
    public async Task<IActionResult> Areas([FromQuery] int governorateId)
    {
        var code = (await _db.StoreSettings.FirstAsync()).CurrencyCode;
        var areas = await _db.DeliveryAreas.Where(a => a.IsActive && a.Governorate!.IsActive && a.GovernorateId == governorateId && a.CurrencyCode == code)
            .OrderBy(a => a.Name).ToListAsync();
        return Ok(ApiResponse<List<DeliveryArea>>.Ok(areas));
    }
}
