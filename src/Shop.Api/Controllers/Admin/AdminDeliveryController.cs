using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Admin;

[ApiController, Authorize(Roles = "Admin"), Route("api/admin/delivery")]
public class AdminDeliveryController : ControllerBase
{
    private readonly ShopContext _db;
    public AdminDeliveryController(ShopContext db) => _db = db;
    private IActionResult Fail(string message, int status = 400) => StatusCode(status, ApiResponse<bool>.Fail(message, status));
    [HttpGet("governorates")]
    public async Task<IActionResult> Governorates() => Ok(ApiResponse<List<Governorate>>.Ok(await _db.Governorates.OrderBy(g => g.Name).ToListAsync()));
    [HttpPost("governorates")]
    public async Task<IActionResult> CreateGovernorate(GovernorateRequest request)
    {
        if (await _db.Governorates.AnyAsync(g => g.Name == request.Name.Trim())) return Fail("Governorate already exists.", 409);
        var item = new Governorate { Name = request.Name.Trim(), IsActive = request.IsActive };
        _db.Governorates.Add(item); await _db.SaveChangesAsync(); return Ok(ApiResponse<Governorate>.Ok(item));
    }
    [HttpPut("governorates/{id:int}")]
    public async Task<IActionResult> UpdateGovernorate(int id, GovernorateRequest request)
    {
        var item = await _db.Governorates.FindAsync(id); if (item is null) return NotFound();
        if (await _db.Governorates.AnyAsync(g => g.Id != id && g.Name == request.Name.Trim())) return Fail("Governorate already exists.", 409);
        item.Name = request.Name.Trim(); item.IsActive = request.IsActive;
        await _db.SaveChangesAsync(); return Ok(ApiResponse<Governorate>.Ok(item));
    }
    [HttpDelete("governorates/{id:int}")]
    public async Task<IActionResult> DeleteGovernorate(int id)
    {
        var item = await _db.Governorates.FindAsync(id); if (item is null) return NotFound();
        if (await _db.DeliveryAreas.AnyAsync(a => a.GovernorateId == id)) return Fail("Governorate contains areas. Disable it instead.", 409);
        _db.Governorates.Remove(item); await _db.SaveChangesAsync(); return Ok(ApiResponse<bool>.Ok(true));
    }
    [HttpGet("areas")]
    public async Task<IActionResult> Areas() => Ok(ApiResponse<List<DeliveryArea>>.Ok(await _db.DeliveryAreas.Include(a => a.Governorate).OrderBy(a => a.Name).ToListAsync()));
    private async Task<string?> ValidateArea(DeliveryAreaRequest request, int id = 0)
    {
        if (decimal.Round(request.ShippingFee, 2) != request.ShippingFee) return "Shipping fees support at most two decimal places.";
        if (!await _db.Governorates.AnyAsync(g => g.Id == request.GovernorateId)) return "Governorate not found.";
        if (!await _db.Currencies.AnyAsync(c => c.Code == request.CurrencyCode.ToUpperInvariant() && c.IsActive)) return "Select an active currency.";
        if (await _db.DeliveryAreas.AnyAsync(a => a.Id != id && a.GovernorateId == request.GovernorateId && a.Name == request.Name.Trim())) return "Area already exists in this governorate.";
        return null;
    }
    [HttpPost("areas")]
    public async Task<IActionResult> CreateArea(DeliveryAreaRequest request)
    {
        var error = await ValidateArea(request); if (error is not null) return Fail(error);
        var item = new DeliveryArea(); Apply(item, request); _db.DeliveryAreas.Add(item);
        await _db.SaveChangesAsync(); return Ok(ApiResponse<DeliveryArea>.Ok(item));
    }
    [HttpPut("areas/{id:int}")]
    public async Task<IActionResult> UpdateArea(int id, DeliveryAreaRequest request)
    {
        var item = await _db.DeliveryAreas.FindAsync(id); if (item is null) return NotFound();
        var error = await ValidateArea(request, id); if (error is not null) return Fail(error);
        Apply(item, request); await _db.SaveChangesAsync(); return Ok(ApiResponse<DeliveryArea>.Ok(item));
    }
    [HttpDelete("areas/{id:int}")]
    public async Task<IActionResult> DeleteArea(int id)
    {
        var item = await _db.DeliveryAreas.FindAsync(id); if (item is null) return NotFound();
        if (await _db.Orders.AnyAsync(o => o.DeliveryAreaId == id)) return Fail("Area is used by orders. Disable it instead.", 409);
        _db.DeliveryAreas.Remove(item); await _db.SaveChangesAsync(); return Ok(ApiResponse<bool>.Ok(true));
    }
    private static void Apply(DeliveryArea item, DeliveryAreaRequest request)
    {
        item.Name = request.Name.Trim(); item.GovernorateId = request.GovernorateId;
        item.ShippingFee = request.ShippingFee; item.CurrencyCode = request.CurrencyCode.ToUpperInvariant(); item.IsActive = request.IsActive;
    }
}
