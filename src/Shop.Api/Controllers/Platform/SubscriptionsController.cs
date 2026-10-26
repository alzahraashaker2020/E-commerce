using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;

namespace Shop.Api.Controllers.Platform;

[ApiController, Authorize(Roles = "PlatformAdmin"), Route("api/platform")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly PlatformContext _db;
    public SubscriptionsController(PlatformContext db) => _db = db;

    [HttpGet("plans")]
    public async Task<IActionResult> Plans() => Ok(ApiResponse<object>.Ok(await _db.Plans.AsNoTracking().OrderBy(p => p.Id).ToListAsync()));

    [HttpPost("plans")]
    public async Task<IActionResult> Create(PlanRequest request)
    {
        if(request.Features is null || request.Features.Any(f=>!PlanFeatures.All.Contains(f)))return BadRequest(ApiResponse<object>.Fail("Select supported plan features.",400));
        if (await _db.Plans.AnyAsync(p => p.Name == request.Name.Trim())) return Conflict(ApiResponse<object>.Fail("Plan name already exists", 409));
        var plan = new SubscriptionPlan(); Apply(plan, request); _db.Plans.Add(plan);
        try { await _db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(ApiResponse<object>.Fail("Plan already exists", 409)); }
        return Ok(ApiResponse<object>.Ok(plan));
    }

    [HttpPut("plans/{id:int}")]
    public async Task<IActionResult> Update(int id, PlanRequest request)
    {
        if(request.Features is null || request.Features.Any(f=>!PlanFeatures.All.Contains(f)))return BadRequest(ApiResponse<object>.Fail("Select supported plan features.",400));
        var plan = await _db.Plans.FindAsync(id);
        if (plan is null) return NotFound();
        if (plan.Version != request.Version) return Conflict(ApiResponse<object>.Fail("Plan changed; refresh before saving", 409));
        if (await _db.Plans.AnyAsync(p => p.Id != id && p.Name == request.Name.Trim())) return Conflict(ApiResponse<object>.Fail("Plan name already exists", 409));
        Apply(plan, request); plan.Version++;
        try { await _db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(ApiResponse<object>.Fail("Plan changed; refresh before saving", 409)); }
        return Ok(ApiResponse<object>.Ok(plan));
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions()
    {
        var stores = await _db.Stores.AsNoTracking().ToListAsync();
        var subscriptions = await _db.Subscriptions.AsNoTracking().ToListAsync();
        return Ok(ApiResponse<object>.Ok(stores.Select(store => new { store.Id, store.Name, store.State,
            Status = SubscriptionAccess.Status(store, subscriptions.SingleOrDefault(s => s.StoreId == store.Id)),
            Subscription = subscriptions.SingleOrDefault(s => s.StoreId == store.Id) })));
    }

    [HttpGet("stores/{storeId}/subscription/history")]
    public async Task<IActionResult> History(string storeId) => Ok(ApiResponse<object>.Ok(await _db.SubscriptionEvents.AsNoTracking()
        .Where(e => e.StoreId == storeId).OrderByDescending(e => e.CreatedAtUtc).Take(100).ToListAsync()));

    [HttpPost("stores/{storeId}/subscription")]
    public async Task<IActionResult> Change(string storeId, ChangeRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var store = await _db.Stores.FindAsync(storeId);
        if (store is null) return NotFound();
        if (store.State is not ("Active" or "Suspended")) return Conflict(ApiResponse<object>.Fail("Store is not ready", 409));
        var current = await _db.Subscriptions.FindAsync(storeId);
        if ((current?.Version ?? 0) != request.Version) return Conflict(ApiResponse<object>.Fail("Subscription changed; refresh before trying again", 409));
        if (request.Action == "Cancel")
        {
            if (current is null || current.IsCancelled) return Conflict(ApiResponse<object>.Fail("No current subscription to cancel", 409));
            current.IsCancelled = true; current.Version++;
        }
        else
        {
            if (request.Action is not ("Assign" or "Trial" or "Renew")) return BadRequest(ApiResponse<object>.Fail("Invalid subscription action", 400));
            var plan = await _db.Plans.FindAsync(request.PlanId);
            if (plan is null || !plan.IsActive) return BadRequest(ApiResponse<object>.Fail("Select an active plan", 400));
            if (request.Action == "Trial" && (store.HasUsedTrial || current is not null || plan.TrialDays == 0))
                return Conflict(ApiResponse<object>.Fail("A trial is available once, before the first subscription", 409));
            if (request.Action == "Renew" && (current is null || current.PlanId != plan.Id))
                return Conflict(ApiResponse<object>.Fail("Renew requires the existing plan; use Assign to change plans", 409));
            DateTime now = DateTime.UtcNow;
            DateTime start = request.Action == "Renew" && current is { IsCancelled: false, IsTrial: false } && current.EndsAtUtc > now ? current.EndsAtUtc : now;
            if (current is null) { current = new StoreSubscription { StoreId = storeId }; _db.Subscriptions.Add(current); }
            else current.Version++;
            current.PlanId = plan.Id; current.PlanName = plan.Name; current.PriceMinor = plan.PriceMinor; current.Currency = plan.Currency;
            current.FeaturesJson=plan.FeaturesJson;
            current.PeriodMonths = plan.PeriodMonths; current.MaxProducts = plan.MaxProducts;
            current.StartsAtUtc = now; current.EndsAtUtc = request.Action == "Trial" ? now.AddDays(plan.TrialDays) : start.AddMonths(plan.PeriodMonths);
            current.IsTrial = request.Action == "Trial"; current.IsCancelled = false;
            if (current.IsTrial) store.HasUsedTrial = true;
        }
        store.SubscriptionRequired = true;
        _db.SubscriptionEvents.Add(new SubscriptionEvent { StoreId = storeId, Action = request.Action, PlanId = current!.PlanId,
            PlanName = current.PlanName, PriceMinor = current.PriceMinor, Currency = current.Currency,
            StartsAtUtc = current.StartsAtUtc, EndsAtUtc = current.EndsAtUtc, ActorId = User.FindFirst("actor_id")?.Value ?? "",
            Reference = request.Reference.Trim() });
        try { await _db.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch (DbUpdateException) { return Conflict(ApiResponse<object>.Fail("Subscription changed; refresh before trying again", 409)); }
        return Ok(ApiResponse<object>.Ok(new { Status = SubscriptionAccess.Status(store, current), Subscription = current }));
    }

    private static void Apply(SubscriptionPlan plan, PlanRequest request)
    {
        plan.NameAr=request.NameAr.Trim();plan.DescriptionAr=request.DescriptionAr.Trim();
        plan.Description=request.Description.Trim();plan.FeaturesJson=System.Text.Json.JsonSerializer.Serialize(request.Features.Distinct());
        plan.Name = request.Name.Trim(); plan.PriceMinor = request.PriceMinor; plan.Currency = request.Currency.ToUpperInvariant();
        plan.PeriodMonths = request.PeriodMonths; plan.TrialDays = request.TrialDays; plan.MaxProducts = request.MaxProducts; plan.IsActive = request.IsActive; plan.AllowMerchantApplications=request.AllowMerchantApplications;
    }
    public sealed class PlanRequest
    {
        [Required, StringLength(100, MinimumLength = 1)] public string Name { get; set; } = "";
        [Range(0, 100000000000L)] public long PriceMinor { get; set; }
        [Required, RegularExpression("^(EGP|USD|EUR|GBP|SAR|AED)$")] public string Currency { get; set; } = "EGP";
        [Range(1, 36)] public int PeriodMonths { get; set; } = 1;
        [Range(0, 90)] public int TrialDays { get; set; }
        [Range(1, 10000000)] public int? MaxProducts { get; set; }
        public bool IsActive { get; set; } = true;
        public bool AllowMerchantApplications { get; set; }
        [MaxLength(100)] public string NameAr{get;set;}="";
        [MaxLength(500)] public string DescriptionAr{get;set;}="";
        [MaxLength(500)] public string Description{get;set;}="";
        [MaxLength(4)]public string[] Features{get;set;}=PlanFeatures.All.ToArray();
        public int Version { get; set; }
    }
    public sealed class ChangeRequest
    {
        [Required] public string Action { get; set; } = "";
        public int PlanId { get; set; }
        [Range(0, int.MaxValue)] public int Version { get; set; }
        [MaxLength(200)] public string Reference { get; set; } = "";
    }
}
