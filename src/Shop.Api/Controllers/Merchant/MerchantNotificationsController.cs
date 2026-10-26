using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Email;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;

namespace Shop.Api.Controllers.Merchant;

[ApiController,Authorize(Roles="Merchant"),Route("api/merchant/notifications")]
public sealed class MerchantNotificationsController:ControllerBase
{
    private readonly ShopContext _db;private readonly IConfiguration _config;private readonly StoreScope _store;
    public MerchantNotificationsController(ShopContext db,IConfiguration config,StoreScope store){_db=db;_config=config;_store=store;}
    [HttpGet]
    public IActionResult Get()
    {
        var options=_config.GetSection(_store.Store?.ConnectionKey=="DefaultStore"?"Email":"StoreEmail:"+_store.StoreId).Get<EmailOptions>()??new();
        StoreEmailPreferences.Read(_db,options);
        bool ready=options.Enabled&&!string.IsNullOrWhiteSpace(options.SmtpHost)&&!string.IsNullOrWhiteSpace(options.SmtpUser)&&!string.IsNullOrWhiteSpace(options.SmtpPassword)&&!string.IsNullOrWhiteSpace(options.From);
        return Ok(ApiResponse<object>.Ok(new {options.MerchantEmail,options.CustomerOrdersEnabled,options.CustomerStatusEnabled,options.MerchantOrdersEnabled,options.From,Delivery=ready?"SMTP configured (delivery not verified)":"Not connected",Ready=ready}));
    }
    [HttpPut]
    public async Task<IActionResult> Update(Preferences request)
    {
        string address=request.MerchantEmail.Trim();
        if(address!=""&&!new EmailAddressAttribute().IsValid(address))return BadRequest(ApiResponse<object>.Fail("Enter a valid merchant notification email.",400));
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"StoreEmailPreferences\" SET \"MerchantEmail\"={0},\"CustomerOrdersEnabled\"={1},\"CustomerStatusEnabled\"={2},\"MerchantOrdersEnabled\"={3} WHERE \"Id\"=1",address,request.CustomerOrdersEnabled,request.CustomerStatusEnabled,request.MerchantOrdersEnabled);
        return Get();
    }
    [HttpGet("messages")]
    public async Task<IActionResult> Messages([FromServices] Shop.Api.Security.EmailOutbox outbox)
    {Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(await outbox.ReadAsync()));}

    [HttpGet("messages/{id}/attempts")]
    public async Task<IActionResult> Attempts(string id)
    {
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var command=_db.Database.GetDbConnection().CreateCommand();
            command.CommandText="SELECT \"AttemptedAtUtc\",\"Outcome\" FROM \"EmailAttempts\" WHERE \"MessageId\"=@id ORDER BY \"AttemptedAtUtc\" DESC LIMIT 100";
            var parameter=command.CreateParameter();parameter.ParameterName="id";parameter.Value=id;command.Parameters.Add(parameter);
            using var reader=await command.ExecuteReaderAsync();var rows=new List<object>();
            while(await reader.ReadAsync())rows.Add(new{AttemptedAtUtc=reader.GetString(0),Outcome=reader.GetString(1)});
            Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(rows));
        }
        finally{await _db.Database.CloseConnectionAsync();}
    }

    [HttpPost("messages/{id}/retry")]
    public async Task<IActionResult> Retry(string id)
    {
        int changed=await _db.Database.ExecuteSqlRawAsync("UPDATE \"EmailOutbox\" SET \"State\"='Pending',\"Attempts\"=0,\"NextAttemptUtc\"={0},\"LastError\"='' WHERE \"Id\"={1} AND \"State\"='Failed' AND \"ExpiresAtUtc\">{0} AND \"Payload\"<>''",DateTime.UtcNow.ToString("O"),id);
        if(changed!=1)return Conflict(ApiResponse<object>.Fail("Only failed, unexpired messages can be retried.",409));
        return Ok(ApiResponse<object>.Ok(new{Queued=true}));
    }

    public sealed class Preferences
    {
        [MaxLength(200)]public string MerchantEmail{get;set;}="";
        public bool CustomerOrdersEnabled{get;set;}=true;
        public bool CustomerStatusEnabled{get;set;}=true;
        public bool MerchantOrdersEnabled{get;set;}=true;
    }
}
