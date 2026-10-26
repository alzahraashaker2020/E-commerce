using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Api.Operations;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;

namespace Shop.Api.Controllers.Merchant;

[ApiController,Authorize(Roles="Merchant"),Route("api/merchant/activity")]
public sealed class MerchantActivityController:ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromServices]OperationJournal journal,[FromServices]StoreScope store)
    {Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok((await journal.ReadAsync(store.StoreId)).Where(e=>e.Role=="Merchant").Select(e=>new{e.Id,e.Actor,e.Action,e.Method,e.Status,e.CreatedAtUtc,e.TraceId})));}
}
