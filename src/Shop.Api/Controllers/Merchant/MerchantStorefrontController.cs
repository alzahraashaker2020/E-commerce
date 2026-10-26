using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;

namespace Shop.Api.Controllers.Merchant;
[ApiController,Authorize(Roles="Merchant"),Route("api/merchant/storefront")]
public sealed class MerchantStorefrontController:ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromServices]StorefrontContent content)=>Ok(ApiResponse<object>.Ok(await content.ReadAsync()));
    [HttpGet("translation-sources")]
    public async Task<IActionResult> TranslationSources([FromServices]StorefrontContent content)=>Ok(ApiResponse<object>.Ok(await content.TranslationSourcesAsync()));
    [HttpPut]
    public async Task<IActionResult> Save(StorefrontContent.Document document,[FromServices]StorefrontContent content)
    {try{if(!await content.SaveAsync(document))return Conflict(ApiResponse<object>.Fail("Storefront changed. Reload before saving.",409));return Ok(ApiResponse<object>.Ok(document));}catch(InvalidOperationException ex){return BadRequest(ApiResponse<object>.Fail(ex.Message,400));}}
    [HttpGet("subscribers")]
    public async Task<IActionResult> Subscribers([FromServices]ShopContext db)
    {
        Response.Headers["Cache-Control"]="no-store";await db.Database.OpenConnectionAsync();
        try{using var cmd=db.Database.GetDbConnection().CreateCommand();cmd.CommandText="SELECT \"Email\",\"JoinedAtUtc\" FROM \"StorefrontSubscribers\" ORDER BY \"JoinedAtUtc\" DESC LIMIT 500";using var r=await cmd.ExecuteReaderAsync();var rows=new List<object>();while(await r.ReadAsync())rows.Add(new{Email=r.GetString(0),JoinedAtUtc=r.GetString(1)});return Ok(ApiResponse<object>.Ok(rows));}finally{await db.Database.CloseConnectionAsync();}
    }
}
