using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Security;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;

namespace Shop.Api.Controllers.Merchant;

[ApiController,Authorize(Roles="Merchant"),Route("api/merchant/returns")]
public sealed class MerchantReturnsController:ControllerBase
{
    private readonly OrderReturnsService _returns;private readonly ShopContext _db;
    public MerchantReturnsController(OrderReturnsService returns,ShopContext db){_returns=returns;_db=db;}
    [HttpGet]
    public async Task<IActionResult> Get(){Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(await _returns.ReadAsync()));}
    [HttpPost("{id}/actions")]
    public async Task<IActionResult> Change(string id,ChangeBody body)
    {
        try{return Ok(ApiResponse<object>.Ok(await _returns.ChangeAsync(id,body.Version,body.Action,body.Notes.Trim(),body.Restock,body.RefundAmount,body.RefundReference.Trim(),User.Identity?.Name??"Merchant")));}
        catch(InvalidOperationException ex){return Conflict(ApiResponse<object>.Fail(ex.Message,409));}
        catch(Npgsql.PostgresException ex)when(ex.SqlState is "40001" or "23505"){return Conflict(ApiResponse<object>.Fail("Return changed concurrently. Refresh and try again.",409));}
        catch(Microsoft.Data.Sqlite.SqliteException ex)when(ex.SqliteErrorCode is 5 or 19){return Conflict(ApiResponse<object>.Fail("Return changed concurrently. Refresh and try again.",409));}
    }
    [HttpGet("{id}/history")]
    public async Task<IActionResult> History(string id)
    {
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var cmd=_db.Database.GetDbConnection().CreateCommand();cmd.CommandText="SELECT \"Action\",\"Actor\",\"Notes\",\"CreatedAtUtc\" FROM \"ReturnEvents\" WHERE \"ReturnId\"=@id ORDER BY \"CreatedAtUtc\"";
            var p=cmd.CreateParameter();p.ParameterName="id";p.Value=id;cmd.Parameters.Add(p);
            using var reader=await cmd.ExecuteReaderAsync();var events=new List<object>();while(await reader.ReadAsync())events.Add(new{Action=reader.GetString(0),Actor=reader.GetString(1),Notes=reader.GetString(2),CreatedAtUtc=reader.GetString(3)});
            Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(events));
        }
        finally{await _db.Database.CloseConnectionAsync();}
    }
    public sealed class ChangeBody
    {
        [Range(1,int.MaxValue)]public int Version{get;set;}
        [Required,MaxLength(30)]public string Action{get;set;}="";
        [Required,MaxLength(1000)]public string Notes{get;set;}="";
        public bool Restock{get;set;}
        public decimal RefundAmount{get;set;}
        [MaxLength(200)]public string RefundReference{get;set;}="";
    }
}
