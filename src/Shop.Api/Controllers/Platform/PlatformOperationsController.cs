using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Api.Operations;
using Shop.BLL.Response_VM;

namespace Shop.Api.Controllers.Platform;

[ApiController,Authorize(Roles="PlatformAdmin"),Route("api/platform/operations")]
public sealed class PlatformOperationsController:ControllerBase
{
    private readonly StoreBackups _backups;private readonly OperationJournal _journal;
    public PlatformOperationsController(StoreBackups backups,OperationJournal journal){_backups=backups;_journal=journal;}
    [HttpGet("backups")]
    public IActionResult Backups([FromQuery]string? storeId=null){Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(_backups.List(storeId).Select(Summary)));}
    [HttpPost("backups/{storeId}")]
    public async Task<IActionResult> Create(string storeId)
    {
        try{var result=await _backups.CreateAsync(storeId,HttpContext.RequestAborted);await _journal.WriteAsync(storeId,User.FindFirst("actor_id")?.Value??"PlatformAdmin","PlatformAdmin","BackupCreated","JOB",200,HttpContext.TraceIdentifier);return Ok(ApiResponse<object>.Ok(Summary(result)));}
        catch(Exception ex)when(ex is not OperationCanceledException){await _journal.WriteAsync(storeId,User.FindFirst("actor_id")?.Value??"PlatformAdmin","PlatformAdmin","BackupFailed","JOB",500,HttpContext.TraceIdentifier,ex.GetType().Name);return Conflict(ApiResponse<object>.Fail("Backup could not complete. Check storage, store configuration and PostgreSQL tools if applicable. Existing backups are preserved.",409));}
    }
    [HttpPost("backups/{id}/verify")]
    public async Task<IActionResult> Verify(string id)
    {
        try{var result=await _backups.VerifyAsync(id,HttpContext.RequestAborted);await _journal.WriteAsync(result.StoreId,User.FindFirst("actor_id")?.Value??"PlatformAdmin","PlatformAdmin","RestoreRehearsalPassed","JOB",200,HttpContext.TraceIdentifier);return Ok(ApiResponse<object>.Ok(Summary(result)));}
        catch(Exception ex)when(ex is not OperationCanceledException){await _journal.WriteAsync("",User.FindFirst("actor_id")?.Value??"PlatformAdmin","PlatformAdmin","RestoreRehearsalFailed","JOB",500,HttpContext.TraceIdentifier,ex.GetType().Name);return Conflict(ApiResponse<object>.Fail("Restore check failed. Verify backup checksums and restore permissions. The running store was not overwritten.",409));}
    }
    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery]string? storeId=null,[FromQuery]bool errorsOnly=false){Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(await _journal.ReadAsync(storeId,errorsOnly)));}
    [HttpPost("events/{id}/acknowledge")]
    public async Task<IActionResult> Acknowledge(string id)
    {return await _journal.AcknowledgeAsync(id,User.FindFirst("actor_id")?.Value??"PlatformAdmin")?Ok(ApiResponse<object>.Ok(true)):Conflict(ApiResponse<object>.Fail("Error not found or already acknowledged.",409));}
    [HttpGet("backup-policy")]
    public async Task<IActionResult> Policy()=>Ok(ApiResponse<object>.Ok(await _journal.ReadPolicyAsync()));
    [HttpPut("backup-policy")]
    public async Task<IActionResult> SetPolicy(PolicyBody body){await _journal.SetPolicyAsync(new(body.Enabled,body.IntervalHours));return await Policy();}
    [HttpPost("stores/{id}/revoke-sessions")]
    public async Task<IActionResult> Revoke(string id,[FromServices]Shop.Api.Tenancy.PlatformContext platform)
    {
        if(await platform.Stores.FindAsync(id) is null)return NotFound(ApiResponse<object>.Fail("Store not found.",404));
        await _journal.RotateSecurityVersionAsync(id);
        await _journal.WriteAsync(id,User.FindFirst("actor_id")?.Value??"PlatformAdmin","PlatformAdmin","StoreSessionsRevoked","JOB",200,HttpContext.TraceIdentifier);
        return Ok(ApiResponse<object>.Ok(true));
    }
    [HttpGet("status")]
    public async Task<IActionResult> Status([FromServices]Shop.Api.Tenancy.PlatformContext platform)
    {
        var policy=await _journal.ReadPolicyAsync();var stores=await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(platform.Stores);
        var rows=stores.Select(s=>new{s.Id,s.Name,s.State,Backup=_backups.List(s.Id).FirstOrDefault()}).Select(s=>new{s.Id,s.Name,s.State,LastBackup=s.Backup?.CreatedAtUtc,LastRestoreCheck=s.Backup?.LastVerifiedAtUtc,RestoreState=s.Backup?.LastVerificationState??"NotChecked",BackupOverdue=s.Backup is null||s.Backup.LastVerificationState=="Failed"||s.Backup.CreatedAtUtc<DateTime.UtcNow.AddHours(-policy.IntervalHours)});
        Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(new{CheckedAtUtc=DateTime.UtcNow,AutomaticBackups=policy.Enabled,policy.IntervalHours,Stores=rows}));
    }
    public sealed class PolicyBody{public bool Enabled{get;set;}[Range(1,168)]public int IntervalHours{get;set;}=24;}
    private static object Summary(StoreBackups.Manifest value)=>new{value.Id,value.StoreId,value.StoreName,value.Provider,value.CreatedAtUtc,value.Bytes,value.LastVerifiedAtUtc,value.LastVerificationState,value.RestoreCheckId,value.Tables,value.Products,value.Orders,FileCount=value.Files.Count};
}
