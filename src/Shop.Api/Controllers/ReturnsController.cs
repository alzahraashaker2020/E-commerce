using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Security;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.Api.Controllers;

[ApiController,Route("api/returns"),EnableRateLimiting("Tracking")]
public sealed class ReturnsController:ControllerBase
{
    private readonly ShopContext _db;private readonly OrderReturnsService _returns;private readonly OrderTrackingAccess _access;
    public ReturnsController(ShopContext db,OrderReturnsService returns,OrderTrackingAccess access){_db=db;_returns=returns;_access=access;}
    private async Task<Order?> Accessible(string number)
    {
        Response.Headers["Cache-Control"]="no-store";Response.Headers["Referrer-Policy"]="no-referrer";
        bool proof=_access.Verify(number,Request.Headers["X-Order-Tracking-Token"].FirstOrDefault());
        int? customer=User.IsInRole("Customer")&&int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
        if(!proof&&customer is null)return null;
        return await _db.Orders.FirstOrDefaultAsync(o=>o.OrderNumber==number&&(proof||o.CustomerId==customer));
    }
    [HttpGet("{orderNumber}")]
    public async Task<IActionResult> Get(string orderNumber)
    {
        var order=await Accessible(orderNumber);if(order is null)return NotFound(ApiResponse<object>.Fail("Order not found or access unavailable.",404));
        var items=await _db.OrderItems.Where(i=>i.OrderId==order.Id).Select(i=>new{i.Id,i.ProductNameSnapshot,i.SizeSnapshot,i.ColorSnapshot,i.Quantity}).ToListAsync();
        return Ok(ApiResponse<object>.Ok(new{order.OrderNumber,Eligible=order.Status==OrderStatus.Delivered&&order.StockDeducted,Items=items,Requests=await _returns.ReadAsync(order.Id)}));
    }
    [HttpPost("{orderNumber}")]
    public async Task<IActionResult> RequestReturn(string orderNumber,RequestBody body)
    {
        var order=await Accessible(orderNumber);if(order is null)return NotFound(ApiResponse<object>.Fail("Order not found or access unavailable.",404));
        if(body.RequestKey==Guid.Empty)return BadRequest(ApiResponse<object>.Fail("A unique return attempt key is required.",400));
        if(body.Reason.Trim().Length<3)return BadRequest(ApiResponse<object>.Fail("Add a return reason of at least three characters.",400));
        try{return Ok(ApiResponse<object>.Ok(await _returns.RequestAsync(order,body.RequestKey,body.OrderItemId,body.Quantity,body.Reason.Trim())));}
        catch(InvalidOperationException ex){return Conflict(ApiResponse<object>.Fail(ex.Message,409));}
        catch(Npgsql.PostgresException ex)when(ex.SqlState is "40001" or "23505"){return Conflict(ApiResponse<object>.Fail("Return changed concurrently. Retry the same request.",409));}
        catch(Microsoft.Data.Sqlite.SqliteException ex)when(ex.SqliteErrorCode is 5 or 19){return Conflict(ApiResponse<object>.Fail("Return changed concurrently. Retry the same request.",409));}
    }
    public sealed class RequestBody
    {
        public Guid RequestKey{get;set;}
        [Range(1,int.MaxValue)]public int OrderItemId{get;set;}
        [Range(1,int.MaxValue)]public int Quantity{get;set;}
        [Required,MinLength(3),MaxLength(1000)]public string Reason{get;set;}="";
    }
}
