using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.DAL.Data;
using Shop.BLL.Response_VM;

namespace Shop.Api.Controllers.Merchant;

[ApiController, Authorize(Roles="Merchant"), Route("api/merchant/orders")]
public sealed class MerchantTrackingAccessController : ControllerBase
{
    [HttpGet("{id:int}/tracking-access")]
    public async Task<IActionResult> Get(int id, [FromServices] ShopContext db, [FromServices] OrderTrackingAccess access)
    {
        Response.Headers.CacheControl="no-store";
        var order=await db.Orders.AsNoTracking().FirstOrDefaultAsync(o=>o.Id==id);
        if(order is null) return NotFound();
        return Ok(ApiResponse<object>.Ok(new {order.OrderNumber,TrackingToken=access.Create(order.OrderNumber)}));
    }
}
