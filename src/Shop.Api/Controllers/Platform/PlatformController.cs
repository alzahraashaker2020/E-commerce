using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.BLL.Security;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.SL.TokenManager;

namespace Shop.Api.Controllers.Platform;

[ApiController, Route("api/platform")]
public sealed class PlatformController : ControllerBase
{
    private readonly PlatformContext _platform;
    private readonly IConfiguration _config;
    private readonly IPasswordHasherService _passwords;
    private readonly ITokenService _tokens;
    public PlatformController(PlatformContext platform, IConfiguration config, IPasswordHasherService passwords, ITokenService tokens)
    { _platform = platform; _config = config; _passwords = passwords; _tokens = tokens; }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _platform.Operators.SingleOrDefaultAsync(a => a.Username == request.Username);
        if (user is null || !_passwords.Verify(user.PasswordHash, request.Password))
            return Unauthorized(ApiResponse<object>.Fail("Invalid credentials", 401));
        var token = _tokens.GeneratePlatformToken(user.Id, user.Username);
        return Ok(ApiResponse<object>.Ok(new { token.Token, token.Expires, user.Username, Role = "PlatformAdmin" }));
    }

    [Authorize(Roles = "PlatformAdmin"), HttpGet("stores")]
    public async Task<IActionResult> Stores() => Ok(ApiResponse<object>.Ok(await _platform.Stores.AsNoTracking()
        .Select(s => new { s.Id, s.Name, s.Host, s.State }).ToListAsync()));

    [Authorize(Roles = "PlatformAdmin"), HttpPost("stores")]
    public async Task<IActionResult> Create(CreateStoreRequest request) => await CreateCore(request);

    [Authorize(Roles="PlatformAdmin"), HttpPost("onboarding/applications/{id}/approve")]
    public async Task<IActionResult> Approve(string id, ApproveRequest request)
    {
        var application=await _platform.MerchantApplications.FindAsync(id);
        if(application is null)return NotFound();
        if(application.State!="Pending" || application.Version!=request.Version)
            return Conflict(ApiResponse<object>.Fail("Application changed. Refresh before reviewing.",409));
        var plan=await _platform.Plans.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==application.PlanId && p.IsActive && p.AllowMerchantApplications);
        if(plan is null)return BadRequest(ApiResponse<object>.Fail("The requested trial is no longer available. Update the plan before approval.",400));
        if(application.TrialDays==0 && application.PriceMinor>0 && (!request.PaymentVerified || string.IsNullOrWhiteSpace(request.PaymentReference)))
            return BadRequest(ApiResponse<object>.Fail("Verify the subscription payment manually and record its reference before activation.",400));
        application.State="Provisioning";application.Version++;
        try{await _platform.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}
        var result=await CreateCore(new CreateStoreRequest {Name=application.StoreName,Host=request.Host,ConnectionKey=request.ConnectionKey,
            OwnerUsername=application.Username,OwnerEmail=application.Email,OwnerPassword="unused-generated-password"}, application, plan, request.PaymentReference);
        if(application.State=="Provisioning")
        {
            // Validation failed before a store was registered: safely return to review.
            application.State="Pending";application.Version++;await _platform.SaveChangesAsync();
        }
        return result;
    }

    private async Task<IActionResult> CreateCore(CreateStoreRequest request, MerchantApplication? application=null, SubscriptionPlan? plan=null, string paymentReference="")
    {
        string host = request.Host.Trim().ToLowerInvariant();
        if(host==(_config["Platform:PublicHost"]??"platform.localhost").ToLowerInvariant())return BadRequest(ApiResponse<object>.Fail("The platform hostname is reserved.",400));
        if (Uri.CheckHostName(host) != UriHostNameType.Dns || host.Contains('/') || host.Contains(':') ||
            !Regex.IsMatch(request.ConnectionKey, "^[A-Za-z0-9_-]{1,80}$") || request.ConnectionKey == "DefaultStore")
            return BadRequest(ApiResponse<object>.Fail("Use a DNS hostname and a configured store connection reference.", 400));
        if (await _platform.Stores.AnyAsync(s => s.Host == host || s.ConnectionKey == request.ConnectionKey))
            return Conflict(ApiResponse<object>.Fail("Store host or database reference is already registered.", 409));
        string identity;
        try { identity = StoreConnections.DatabaseIdentity(_config, request.ConnectionKey); }
        catch (InvalidOperationException) { return BadRequest(ApiResponse<object>.Fail("Database reference must be configured on the server first.", 400)); }
        foreach (var key in await _platform.Stores.Select(s => s.ConnectionKey).ToListAsync())
            if (StoreConnections.DatabaseIdentity(_config, key) == identity)
                return Conflict(ApiResponse<object>.Fail("Each store must have a separate database.", 409));
        // Provision only a new database; never clean or adopt an existing merchant database.
        var options = new DbContextOptionsBuilder<ShopContext>();
        StoreConnections.Configure(options, _config, request.ConnectionKey);
        using var db = new ShopContext(options.Options);
        var (provider, connection) = StoreConnections.Read(_config, request.ConnectionKey);
        if (provider == "Sqlite" && System.IO.File.Exists(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connection).DataSource))
            return Conflict(ApiResponse<object>.Fail("Provisioning requires a new, empty database path.", 409));
        if (provider == "Postgres" && await db.Database.CanConnectAsync())
        {
            await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public'";
            if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 0)
                return Conflict(ApiResponse<object>.Fail("Provisioning requires an empty database.", 409));
            await db.Database.CloseConnectionAsync();
        }
        var store = new StoreRegistration { Name = request.Name.Trim(), Host = host, ConnectionKey = request.ConnectionKey, DatabaseIdentity = identity, SubscriptionRequired = true };
        _platform.Stores.Add(store);
        try { await _platform.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(ApiResponse<object>.Fail("Store host or database was registered concurrently.", 409)); }
        try
        {
            if (provider == "Postgres") await db.Database.MigrateAsync();
            else { bool created = await db.Database.EnsureCreatedAsync(); SqliteSchemaUpgrade.Apply(db, created); }
            await using var transaction = await db.Database.BeginTransactionAsync();
            // Seed data is for the original demonstration only. A new merchant starts empty.
            foreach (string table in new[] { "Reviews", "OrderItems", "Orders", "CartItems", "Carts", "Customers", "ProductImages", "ProductVariants", "Products", "Discounts", "AdminUsers" })
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\"");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Categories\" WHERE \"ParentCategoryId\" IS NOT NULL");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Categories\"");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Sizes\"");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Colors\"");
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Merchant" || r.Name == "Admin");
            if (role is null) { role = new Role { Name = "Merchant" }; db.Roles.Add(role); await db.SaveChangesAsync(); }
            else role.Name = "Merchant";
            db.AdminUsers.Add(new AdminUser { Username = request.OwnerUsername.Trim(), Email = request.OwnerEmail.Trim(),
                PasswordHash = application?.PasswordHash ?? _passwords.Hash(request.OwnerPassword), RoleId = role.Id, IsActive = true });
            await db.SaveChangesAsync();
            if(application is not null)
            {
                var doc=new StorefrontContent.Document {BrandName=application.StoreName,BusinessMode=application.BusinessMode};
                var cms=new StorefrontContent(db,new StoreScope {Store=store});
                if(!await cms.SaveAsync(doc))throw new InvalidOperationException("Store content could not be initialized.");
            }
            await transaction.CommitAsync();
            if(application is not null && plan is not null)
            {
                var now=DateTime.UtcNow;bool trial=application.TrialDays>0;var end=trial?now.AddDays(application.TrialDays):now.AddMonths(application.PeriodMonths);
                _platform.Subscriptions.Add(new StoreSubscription {StoreId=store.Id,PlanId=plan.Id,PlanName=application.PlanName,PriceMinor=application.PriceMinor,Currency=application.Currency,
                    PeriodMonths=application.PeriodMonths,FeaturesJson=application.FeaturesJson,MaxProducts=application.MaxProducts,StartsAtUtc=now,EndsAtUtc=end,IsTrial=trial});
                _platform.SubscriptionEvents.Add(new SubscriptionEvent {StoreId=store.Id,Action=trial?"Trial":"Assign",PlanId=plan.Id,PlanName=application.PlanName,PriceMinor=application.PriceMinor,
                    Currency=application.Currency,StartsAtUtc=now,EndsAtUtc=end,ActorId=User.FindFirst("actor_id")?.Value??User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value??"platform",
                    Reference="Approved merchant application "+application.Id+(paymentReference!=""?"; "+paymentReference:"")});
                store.HasUsedTrial=trial;
                application.State="Approved";application.StoreId=store.Id;application.ReviewedAtUtc=now;
                application.ReviewedBy=User.FindFirst("actor_id")?.Value??User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value??"platform";application.Version++;
            }
            store.State = "Active"; await _platform.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { store.Id, store.Name, store.Host, store.State }));
        }
        catch
        {
            // Do not commit a partially granted trial if final platform persistence failed.
            _platform.ChangeTracker.Clear();
            store=await _platform.Stores.SingleAsync(s=>s.Id==store.Id);
            store.State="Failed";
            if(application is not null)
            {
                var failed=await _platform.MerchantApplications.SingleAsync(a=>a.Id==application.Id);
                failed.State="Failed";failed.StoreId=store.Id;failed.ReviewNote="Store setup requires operator attention.";failed.Version++;
                application.State="Failed";
            }
            await _platform.SaveChangesAsync();
            return StatusCode(500, ApiResponse<object>.Fail("Provisioning failed. Store remains unavailable; inspect server configuration before retrying.", 500));
        }
    }

    [Authorize(Roles = "PlatformAdmin"), HttpPut("stores/{id}/state")]
    public async Task<IActionResult> State(string id, StateRequest request)
    {
        var store = await _platform.Stores.FindAsync(id);
        if (store is null) return NotFound();
        if (request.State is not ("Active" or "Suspended") || store.State is not ("Active" or "Suspended"))
            return BadRequest(ApiResponse<object>.Fail("Only ready stores can be activated or suspended.", 400));
        store.State = request.State; await _platform.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { store.Id, store.State }));
    }

    public sealed class ApproveRequest
    {
        public int Version{get;set;}
        public bool PaymentVerified{get;set;}
        [MaxLength(200)]public string PaymentReference{get;set;}="";
        [Required,MaxLength(253)] public string Host{get;set;}="";
        [Required,MaxLength(80)] public string ConnectionKey{get;set;}="";
    }
    public sealed class LoginRequest
    {
        [Required, MaxLength(100)] public string Username { get; set; } = "";
        [Required, MaxLength(200)] public string Password { get; set; } = "";
    }
    public sealed class CreateStoreRequest
    {
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        [Required, MaxLength(253)] public string Host { get; set; } = "";
        [Required, MaxLength(80)] public string ConnectionKey { get; set; } = "";
        [Required, MaxLength(100)] public string OwnerUsername { get; set; } = "";
        [Required, EmailAddress, MaxLength(200)] public string OwnerEmail { get; set; } = "";
        [Required, MinLength(12), MaxLength(200)] public string OwnerPassword { get; set; } = "";
    }
    public sealed class StateRequest { [Required] public string State { get; set; } = ""; }
}
