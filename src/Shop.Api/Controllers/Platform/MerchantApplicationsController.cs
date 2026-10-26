using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.BLL.Security;

namespace Shop.Api.Controllers.Platform;

[ApiController, Route("api/platform/onboarding")]
public sealed class MerchantApplicationsController : ControllerBase
{
    private readonly PlatformContext _db;
    private readonly IPasswordHasherService _passwords;
    public MerchantApplicationsController(PlatformContext db, IPasswordHasherService passwords) { _db=db; _passwords=passwords; }

    [HttpGet("plans")]
    public async Task<IActionResult> Plans() => Ok(ApiResponse<object>.Ok(await _db.Plans.AsNoTracking()
        .Where(p=>p.IsActive && p.AllowMerchantApplications)
        .Select(p=>new {p.Id,p.Name,p.TrialDays,p.MaxProducts,p.Currency,p.PriceMinor,p.PeriodMonths,p.Description,p.NameAr,p.DescriptionAr,p.FeaturesJson,p.Version}).ToListAsync()));

    [HttpPost("applications"), EnableRateLimiting("MerchantRegistration")]
    public async Task<IActionResult> Submit(ApplyRequest request)
    {
        if (!request.AcceptTerms || request.BusinessMode is not ("Commerce" or "Catalog") ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.Username,"^[A-Za-z0-9_-]{3,60}$"))
            return BadRequest(ApiResponse<object>.Fail("Check your merchant details and accept the application terms.",400));
        var plan=await _db.Plans.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==request.PlanId && p.IsActive && p.AllowMerchantApplications);
        if (plan is null || plan.Version!=request.PlanVersion)
            return BadRequest(ApiResponse<object>.Fail("Select an available published plan.",400));
        var application=new MerchantApplication {FullName=request.FullName.Trim(), Email=request.Email.Trim().ToLowerInvariant(),
            Phone=request.Phone.Trim(), Username=request.Username.Trim(), PasswordHash=_passwords.Hash(request.Password),
            StoreName=request.StoreName.Trim(),BusinessCategory=request.BusinessCategory.Trim(),BusinessDescription=request.BusinessDescription.Trim(),
            BusinessMode=request.BusinessMode,PlanId=plan.Id,PlanName=plan.Name,TrialDays=plan.TrialDays,MaxProducts=plan.MaxProducts,PriceMinor=plan.PriceMinor,Currency=plan.Currency,PeriodMonths=plan.PeriodMonths,FeaturesJson=plan.FeaturesJson};
        _db.MerchantApplications.Add(application);
        try { await _db.SaveChangesAsync(); }
        catch(DbUpdateException) { return Conflict(ApiResponse<object>.Fail("An application already exists for this email. Check its status using your application password.",409)); }
        return Ok(ApiResponse<object>.Ok(new {application.Id,application.State}));
    }

    [HttpPost("status"), EnableRateLimiting("MerchantApplicationStatus")]
    public async Task<IActionResult> Status(StatusRequest request)
    {
        var app=await _db.MerchantApplications.AsNoTracking().SingleOrDefaultAsync(a=>a.Email==request.Email.Trim().ToLowerInvariant());
        if(app is null || !_passwords.Verify(app.PasswordHash,request.Password))
            return Unauthorized(ApiResponse<object>.Fail("Invalid email or password.",401));
        var host=app.State=="Approved" ? await _db.Stores.Where(s=>s.Id==app.StoreId).Select(s=>s.Host).SingleOrDefaultAsync() : null;
        return Ok(ApiResponse<object>.Ok(new {app.Id,app.State,app.StoreName,app.Username,app.ReviewNote,app.PlanName,app.TrialDays,app.MaxProducts,app.PriceMinor,app.Currency,app.PeriodMonths,app.FeaturesJson,Host=host}));
    }

    [Authorize(Roles="PlatformAdmin"), HttpGet("applications")]
    public async Task<IActionResult> List() => Ok(ApiResponse<object>.Ok(await _db.MerchantApplications.AsNoTracking()
        .OrderBy(a=>a.State!="Pending").ThenByDescending(a=>a.CreatedAtUtc).Take(200)
        .Select(a=>new {a.Id,a.FullName,a.Email,a.Phone,a.Username,a.StoreName,a.BusinessCategory,a.BusinessDescription,a.BusinessMode,
            a.PlanId,a.PlanName,a.TrialDays,a.MaxProducts,a.PriceMinor,a.Currency,a.PeriodMonths,a.FeaturesJson,a.State,a.ReviewNote,a.StoreId,a.CreatedAtUtc,a.ReviewedAtUtc,a.ReviewedBy,a.Version}).ToListAsync()));

    [Authorize(Roles="PlatformAdmin"), HttpPost("applications/{id}/reject")]
    public async Task<IActionResult> Reject(string id, RejectRequest request)
    {
        var app=await _db.MerchantApplications.FindAsync(id);
        if(app is null)return NotFound();
        if(app.State!="Pending" || app.Version!=request.Version)return Conflict(ApiResponse<object>.Fail("Application changed. Refresh before reviewing.",409));
        app.State="Rejected"; app.ReviewNote=request.Note.Trim();app.ReviewedAtUtc=DateTime.UtcNow;
        app.ReviewedBy=User.FindFirstValue("actor_id")??User.FindFirstValue(ClaimTypes.NameIdentifier)??"platform";app.Version++;
        try{await _db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}
        return Ok(ApiResponse<object>.Ok(new {app.Id,app.State}));
    }
    public sealed class ApplyRequest
    {
        [Required,MaxLength(150)] public string FullName{get;set;}="";
        [Required,EmailAddress,MaxLength(200)] public string Email{get;set;}="";
        [Required,MaxLength(40)] public string Phone{get;set;}="";
        [Required,MaxLength(60)] public string Username{get;set;}="";
        [Required,MinLength(12),MaxLength(200)] public string Password{get;set;}="";
        [Required,MaxLength(150)] public string StoreName{get;set;}="";
        [Required,MaxLength(100)] public string BusinessCategory{get;set;}="";
        [Required,MaxLength(1000)] public string BusinessDescription{get;set;}="";
        [Required] public string BusinessMode{get;set;}="Commerce";
        public int PlanId{get;set;}
        [Range(1,int.MaxValue)] public int PlanVersion{get;set;}
        public bool AcceptTerms{get;set;}
    }
    public sealed class StatusRequest
    {
        [Required,EmailAddress,MaxLength(200)] public string Email{get;set;}="";
        [Required,MaxLength(200)] public string Password{get;set;}="";
    }
    public sealed class RejectRequest
    {
        public int Version{get;set;}
        [Required,MaxLength(500)] public string Note{get;set;}="";
    }
}
