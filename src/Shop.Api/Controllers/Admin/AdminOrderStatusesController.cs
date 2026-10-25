using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Admin;

[ApiController, Authorize(Roles = "Admin"), Route("api/admin/order-statuses")]
public class AdminOrderStatusesController : ControllerBase
{
    private readonly ShopContext _db;
    public AdminOrderStatusesController(ShopContext db) => _db = db;
    [HttpGet]
    public async Task<IActionResult> List() => Ok(ApiResponse<List<OrderStatusDefinition>>.Ok(await _db.OrderStatusDefinitions.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id).ToListAsync()));
    [HttpGet("behaviors")]
    public IActionResult Behaviors() => Ok(ApiResponse<string[]>.Ok(Enum.GetNames<OrderStatus>()));
    [HttpPost]
    public async Task<IActionResult> Create(StatusDefinitionRequest request)
    {
        if (await _db.OrderStatusDefinitions.AnyAsync(s => s.Name == request.Name.Trim())) return Conflict(ApiResponse<bool>.Fail("Status name already exists.", 409));
        var item = new OrderStatusDefinition(); Apply(item, request);
        _db.OrderStatusDefinitions.Add(item); await _db.SaveChangesAsync(); return Ok(ApiResponse<OrderStatusDefinition>.Ok(item));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, StatusDefinitionRequest request)
    {
        var item = await _db.OrderStatusDefinitions.FindAsync(id);
        if (item is null) return NotFound();
        if (item.IsDefault && (!request.IsActive || request.Behavior != OrderStatus.Pending)) return Conflict(ApiResponse<bool>.Fail("The initial order status must stay active with Pending behavior.", 409));
        if (item.Behavior != request.Behavior && await _db.Orders.AnyAsync(o => o.StatusDefinitionId == id)) return Conflict(ApiResponse<bool>.Fail("The behavior of an in-use status cannot change.", 409));
        if (await _db.OrderStatusDefinitions.AnyAsync(s => s.Id != id && s.Name == request.Name.Trim())) return Conflict(ApiResponse<bool>.Fail("Status name already exists.", 409));
        Apply(item, request); await _db.SaveChangesAsync(); return Ok(ApiResponse<OrderStatusDefinition>.Ok(item));
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.OrderStatusDefinitions.FindAsync(id);
        if (item is null) return NotFound();
        if (item.IsDefault || await _db.Orders.AnyAsync(o => o.StatusDefinitionId == id)) return Conflict(ApiResponse<bool>.Fail("Status is in use or is the initial status. It cannot be deleted.", 409));
        _db.OrderStatusDefinitions.Remove(item); await _db.SaveChangesAsync(); return Ok(ApiResponse<bool>.Ok(true));
    }
    private static void Apply(OrderStatusDefinition item, StatusDefinitionRequest request)
    {
        item.Name = request.Name.Trim(); item.Color = request.Color; item.Behavior = request.Behavior;
        item.DisplayOrder = request.DisplayOrder; item.IsActive = request.IsActive;
    }
}
