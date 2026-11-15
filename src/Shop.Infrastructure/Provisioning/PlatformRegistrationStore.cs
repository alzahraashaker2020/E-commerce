using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Application.Provisioning;
using Shop.Application.Common;
using Shop.Api.Tenancy;
using Shop.BLL.Security;
using Shop.DAL.Data;
using Shop.DAL.Models;
using System.Text.RegularExpressions;
namespace Shop.Infrastructure.Provisioning;

public sealed class PlatformRegistrationStore : IPlatformRegistrationStore, IAsyncDisposable
{
    private readonly PlatformContext _platform; private readonly IConfiguration _config; private readonly IPasswordHasherService _passwords; private readonly IClock _clock;
    private MerchantApplication? _application; private SubscriptionPlan? _plan; private StoreRegistration? _store; private ShopContext? _tenant; private string _host = "", _identity = "", _provider = "", _connection = "";
    public PlatformRegistrationStore(PlatformContext platform, IConfiguration config, IPasswordHasherService passwords, IClock clock) { _platform = platform; _config = config; _passwords = passwords; _clock = clock; }
    public async Task<ApprovalPreparation> PrepareApprovalAsync(ApprovalCommand command)
    {
        var application = await _platform.MerchantApplications.FindAsync(command.Id);
        if (application is null) return new(null, new(ProvisioningOutcomeKind.NotFound, Bare: true));
        if (application.State != "Pending" || application.Version != command.Version)
            return new(null, new(ProvisioningOutcomeKind.Conflict, "Application changed. Refresh before reviewing."));
        var plan = await _platform.Plans.AsNoTracking().SingleOrDefaultAsync(p => p.Id == application.PlanId && p.IsActive && p.AllowMerchantApplications);
        if (plan is null) return new(null, new(ProvisioningOutcomeKind.Invalid, "The requested trial is no longer available. Update the plan before approval."));
        if (application.TrialDays == 0 && application.PriceMinor > 0 && (!command.PaymentVerified || string.IsNullOrWhiteSpace(command.PaymentReference)))
            return new(null, new(ProvisioningOutcomeKind.Invalid, "Verify the subscription payment manually and record its reference before activation."));
        application.State = "Provisioning"; application.Version++;
        try { await _platform.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return new(null, new(ProvisioningOutcomeKind.Conflict, Bare: true)); }

        _application = application; _plan = plan;
        return new(new ProvisioningRequest { Name = application.StoreName, Host = command.Host, ConnectionKey = command.ConnectionKey, OwnerUsername = application.Username, OwnerEmail = application.Email, OwnerPassword = "unused-generated-password" }, null);
    }
    public async Task<ProvisioningOutcome?> ValidateAsync(ProvisioningRequest request)
    {
        _host = request.Host.Trim().ToLowerInvariant();
        if (_host == (_config["Platform:PublicHost"] ?? "platform.localhost").ToLowerInvariant()) return new(ProvisioningOutcomeKind.Invalid, "The platform hostname is reserved.");
        if (Uri.CheckHostName(_host) != UriHostNameType.Dns || _host.Contains('/') || _host.Contains(':') ||
            !Regex.IsMatch(request.ConnectionKey, "^[A-Za-z0-9_-]{1,80}$") || request.ConnectionKey == "DefaultStore")
            return new(ProvisioningOutcomeKind.Invalid, "Use a DNS hostname and a configured store connection reference.");
        if (await _platform.Stores.AnyAsync(s => s.Host == _host || s.ConnectionKey == request.ConnectionKey))
            return new(ProvisioningOutcomeKind.Conflict, "Store host or database reference is already registered.");

        try { _identity = StoreConnections.DatabaseIdentity(_config, request.ConnectionKey); }
        catch (InvalidOperationException) { return new(ProvisioningOutcomeKind.Invalid, "Database reference must be configured on the server first."); }
        foreach (var key in await _platform.Stores.Select(s => s.ConnectionKey).ToListAsync())
            if (StoreConnections.DatabaseIdentity(_config, key) == _identity)
                return new(ProvisioningOutcomeKind.Conflict, "Each store must have a separate database.");
        // Provision only a new database; never clean or adopt an existing merchant database.
        var options = new DbContextOptionsBuilder<ShopContext>();
        StoreConnections.Configure(options, _config, request.ConnectionKey);
        _tenant = new ShopContext(options.Options);
        var db = _tenant;
        (_provider, _connection) = StoreConnections.Read(_config, request.ConnectionKey);
        var provider = _provider; var connection = _connection;
        if (provider == "Sqlite" && System.IO.File.Exists(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connection).DataSource))
            return new(ProvisioningOutcomeKind.Conflict, "Provisioning requires a new, empty database path.");
        if(provider=="SqlServer" && SqlServerTenantScope.Schema(connection) is not null)
        {
            if(await SqlServerTenantScope.ExistsAsync(connection)) return new(ProvisioningOutcomeKind.Conflict,"Provisioning requires a new tenant schema.");
        }
        else if (provider is "Postgres" or "SqlServer" && await db.Database.CanConnectAsync())
        {
            await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = provider == "SqlServer" ? "SELECT COUNT(*) FROM sys.tables" : "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public'";
            if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 0)
                return new(ProvisioningOutcomeKind.Conflict, "Provisioning requires an empty database.");
            await db.Database.CloseConnectionAsync();
        }

        return null;
    }
    public async Task<ProvisioningOutcome?> RegisterAsync(ProvisioningRequest request)
    {
        _store = new StoreRegistration { Name = request.Name.Trim(), Host = _host, ConnectionKey = request.ConnectionKey, DatabaseIdentity = _identity, SubscriptionRequired = true }; _platform.Stores.Add(_store);
        try { await _platform.SaveChangesAsync(); } catch (DbUpdateException) { return new(ProvisioningOutcomeKind.Conflict, "Store host or database was registered concurrently."); }
        return null;
    }
    public async Task InitializeTenantAsync(ProvisioningRequest request)
    {
        var provider = _provider; var db = _tenant!; var application = _application; var store = _store!;
        if (provider == "Postgres") await db.Database.MigrateAsync();
        else if (provider == "SqlServer" && SqlServerTenantScope.Schema(_connection) is not null) await SqlServerTenantScope.CreateAsync(_connection);
        else if (provider == "SqlServer") { bool created = await db.Database.EnsureCreatedAsync(); SqlServerSchema.Apply(db, created); }
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
        db.AdminUsers.Add(new AdminUser
        {
            Username = request.OwnerUsername.Trim(),
            Email = request.OwnerEmail.Trim(),
            PasswordHash = application?.PasswordHash ?? _passwords.Hash(request.OwnerPassword),
            RoleId = role.Id,
            IsActive = true
        });
        await db.SaveChangesAsync();
        if (application is not null)
        {
            var doc = new StorefrontContent.Document { BrandName = application.StoreName, BusinessMode = application.BusinessMode };
            var cms = new StorefrontContent(db, new StoreScope { Store = store });
            if (!await cms.SaveAsync(doc)) throw new InvalidOperationException("Store content could not be initialized.");
        }
        await transaction.CommitAsync();

    }
    public async Task<ProvisionedStore> ActivateAsync(string actor, string paymentReference)
    {
        var application = _application; var plan = _plan; var store = _store!;
        if (application is not null && plan is not null)
        {
            var now = _clock.UtcNow; bool trial = application.TrialDays > 0; var end = trial ? now.AddDays(application.TrialDays) : now.AddMonths(application.PeriodMonths);
            _platform.Subscriptions.Add(new StoreSubscription
            {
                StoreId = store.Id,
                PlanId = plan.Id,
                PlanName = application.PlanName,
                PriceMinor = application.PriceMinor,
                Currency = application.Currency,
                PeriodMonths = application.PeriodMonths,
                FeaturesJson = application.FeaturesJson,
                MaxProducts = application.MaxProducts,
                StartsAtUtc = now,
                EndsAtUtc = end,
                IsTrial = trial
            });
            _platform.SubscriptionEvents.Add(new SubscriptionEvent
            {
                StoreId = store.Id,
                Action = trial ? "Trial" : "Assign",
                PlanId = plan.Id,
                PlanName = application.PlanName,
                PriceMinor = application.PriceMinor,
                Currency = application.Currency,
                StartsAtUtc = now,
                EndsAtUtc = end,
                ActorId = actor,
                Reference = "Approved merchant application " + application.Id + (paymentReference != "" ? "; " + paymentReference : "")
            });
            store.HasUsedTrial = trial;
            application.State = "Approved"; application.StoreId = store.Id; application.ReviewedAtUtc = now;
            application.ReviewedBy = actor; application.Version++;
        }
        store.State = "Active"; await _platform.SaveChangesAsync();

        return new(store.Id, store.Name, store.Host, store.State);
    }
    public async Task FailAsync()
    {
        var store = _store!; var application = _application;
        // Do not commit a partially granted trial if final platform persistence failed.
        _platform.ChangeTracker.Clear();
        store = await _platform.Stores.SingleAsync(s => s.Id == store.Id);
        store.State = "Failed";
        if (application is not null)
        {
            var failed = await _platform.MerchantApplications.SingleAsync(a => a.Id == application.Id);
            failed.State = "Failed"; failed.StoreId = store.Id; failed.ReviewNote = "Store setup requires operator attention."; failed.Version++;
            application.State = "Failed";
        }
        await _platform.SaveChangesAsync();

        _store = store;
    }
    public async Task ResetApprovalIfProvisioningAsync()
    {
        if (_application?.State == "Provisioning") { _application.State = "Pending"; _application.Version++; await _platform.SaveChangesAsync(); }
    }
    public async ValueTask DisposeAsync() { if (_tenant is not null) await _tenant.DisposeAsync(); }
}
