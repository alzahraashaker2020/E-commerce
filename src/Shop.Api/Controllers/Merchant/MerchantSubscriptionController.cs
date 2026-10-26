using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;

namespace Shop.Api.Controllers.Merchant;

[ApiController, Authorize(Roles = "Merchant"), Route("api/merchant/subscription")]
public sealed class MerchantSubscriptionController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromServices] StoreScope scope, [FromServices] ShopContext db) =>
        Ok(ApiResponse<object>.Ok(new { Status = SubscriptionAccess.Status(scope.Store!, scope.Subscription),
            Subscription = scope.Subscription, ProductCount = await db.Products.CountAsync(),
            CanSell = !scope.Store!.SubscriptionRequired || SubscriptionAccess.IsCurrent(scope.Subscription) }));
}
