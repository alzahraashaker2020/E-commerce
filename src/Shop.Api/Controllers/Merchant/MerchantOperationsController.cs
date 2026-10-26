using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Api.Operations;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;

namespace Shop.Api.Controllers.Merchant;

/// <summary>Local owner operations in standalone installations; never exposes platform or other-store data.</summary>
[ApiController, Authorize(Roles="Merchant"), Route("api/merchant/operations")]
public sealed class MerchantOperationsController : ControllerBase
{
    private readonly StoreScope _store;
    private readonly StoreBackups _backups;
    private readonly OperationJournal _journal;
    public MerchantOperationsController(StoreScope store, StoreBackups backups, OperationJournal journal)
    { _store=store; _backups=backups; _journal=journal; }
    private string Actor=>User.FindFirst("actor_id")?.Value??"Merchant";
    private static object Summary(StoreBackups.Manifest m)=>new{m.Id,m.StoreId,m.StoreName,m.Provider,m.CreatedAtUtc,m.Bytes,m.LastVerifiedAtUtc,m.LastVerificationState,m.RestoreCheckId,m.Tables,m.Products,m.Orders,FileCount=m.Files.Count};

    [HttpGet("backups")]
    public IActionResult Backups(){Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(_backups.List(_store.StoreId).Select(Summary)));}
    [HttpPost("backups/{storeId}")]
    public async Task<IActionResult> Create(string storeId)
    {
        if(storeId!=_store.StoreId)return NotFound();
        try{var result=await _backups.CreateAsync(_store.StoreId,HttpContext.RequestAborted);await _journal.WriteAsync(_store.StoreId,Actor,"Merchant","BackupCreated","JOB",200,HttpContext.TraceIdentifier);return Ok(ApiResponse<object>.Ok(Summary(result)));}
        catch(Exception ex)when(ex is not OperationCanceledException){return await Failed("BackupFailed",ex,"Backup could not complete. Check storage and database tools. Existing backups are preserved.");}
    }
    [HttpPost("backups/{id}/verify")]
    public async Task<IActionResult> Verify(string id)
    {
        if(!_backups.List(_store.StoreId).Any(b=>b.Id==id))return NotFound();
        try{var result=await _backups.VerifyAsync(id,HttpContext.RequestAborted);await _journal.WriteAsync(_store.StoreId,Actor,"Merchant","RestoreRehearsalPassed","JOB",200,HttpContext.TraceIdentifier);return Ok(ApiResponse<object>.Ok(Summary(result)));}
        catch(Exception ex)when(ex is not OperationCanceledException){return await Failed("RestoreRehearsalFailed",ex,"Restore check failed. The running store was not overwritten.");}
    }
    private async Task<IActionResult> Failed(string action,Exception error,string message)
    {await _journal.WriteAsync(_store.StoreId,Actor,"Merchant",action,"JOB",500,HttpContext.TraceIdentifier,error.GetType().Name);return Conflict(ApiResponse<object>.Fail(message,409));}
    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery]bool errorsOnly=false)
    {Response.Headers["Cache-Control"]="no-store";return Ok(ApiResponse<object>.Ok(await _journal.ReadAsync(_store.StoreId,errorsOnly)));}
    [HttpPost("events/{id}/acknowledge")]
    public async Task<IActionResult> Acknowledge(string id)=>await _journal.AcknowledgeAsync(id,Actor,_store.StoreId)?Ok(ApiResponse<object>.Ok(true)):NotFound();
    [HttpGet("backup-policy")]
    public async Task<IActionResult> Policy()=>Ok(ApiResponse<object>.Ok(await _journal.ReadPolicyAsync()));
    [HttpPut("backup-policy")]
    public async Task<IActionResult> SetPolicy(OwnerBackupPolicy body){await _journal.SetPolicyAsync(new(body.Enabled,body.IntervalHours));return await Policy();}
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        Response.Headers["Cache-Control"]="no-store";
        var policy=await _journal.ReadPolicyAsync();var backup=_backups.List(_store.StoreId).FirstOrDefault();
        return Ok(ApiResponse<object>.Ok(new{CheckedAtUtc=DateTime.UtcNow,AutomaticBackups=policy.Enabled,policy.IntervalHours,Stores=new[]{new{_store.Store!.Id,_store.Store.Name,_store.Store.State,LastBackup=backup?.CreatedAtUtc,LastRestoreCheck=backup?.LastVerifiedAtUtc,RestoreState=backup?.LastVerificationState??"NotChecked",BackupOverdue=backup is null||backup.LastVerificationState=="Failed"||backup.CreatedAtUtc<DateTime.UtcNow.AddHours(-policy.IntervalHours)}}}));
    }
    public sealed class OwnerBackupPolicy{public bool Enabled{get;set;}[Range(1,168)]public int IntervalHours{get;set;}=24;}
}
