using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Merchant;

[ApiController, Authorize(Roles = "Merchant"), Route("api/merchant/currencies")]
public class MerchantCurrenciesController : ControllerBase
{
    private readonly ShopContext _db;
    public MerchantCurrenciesController(ShopContext db) => _db = db;
    [HttpGet]
    public async Task<IActionResult> List() => Ok(ApiResponse<List<Currency>>.Ok(await _db.Currencies.OrderBy(c => c.Code).ToListAsync()));
    [HttpPost]
    public async Task<IActionResult> Create(CurrencyRequest request)
    {
        var code = request.Code.ToUpperInvariant();
        if (await _db.Currencies.AnyAsync(c => c.Code == code)) return Conflict(ApiResponse<bool>.Fail("Currency code already exists.", 409));
        var item = new Currency { Code = code, Name = request.Name.Trim(), Symbol = request.Symbol.Trim(), IsActive = request.IsActive };
        _db.Currencies.Add(item); await _db.SaveChangesAsync();
        return Ok(ApiResponse<Currency>.Ok(item));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CurrencyRequest request)
    {
        var item = await _db.Currencies.FindAsync(id);
        if (item is null) return NotFound();
        if (request.Code.ToUpperInvariant() != item.Code) return Conflict(ApiResponse<bool>.Fail("Currency codes cannot change. Add a new currency instead.", 409));
        var settings = await _db.StoreSettings.FirstAsync();
        if (!request.IsActive && settings.CurrencyCode == item.Code) return Conflict(ApiResponse<bool>.Fail("Select another store currency before disabling this currency.", 409));
        item.Name = request.Name.Trim(); item.Symbol = request.Symbol.Trim(); item.IsActive = request.IsActive;
        if (settings.CurrencyCode == item.Code) settings.CurrencySymbol = item.Symbol;
        await _db.SaveChangesAsync(); return Ok(ApiResponse<Currency>.Ok(item));
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Currencies.FindAsync(id);
        if (item is null) return NotFound();
        if (await _db.StoreSettings.AnyAsync(s => s.CurrencyCode == item.Code) || await _db.Orders.AnyAsync(o => o.CurrencyCode == item.Code) || await _db.DeliveryAreas.AnyAsync(a => a.CurrencyCode == item.Code))
            return Conflict(ApiResponse<bool>.Fail("Currency is in use. Disable it instead of deleting it.", 409));
        _db.Currencies.Remove(item); await _db.SaveChangesAsync(); return Ok(ApiResponse<bool>.Ok(true));
    }
}
