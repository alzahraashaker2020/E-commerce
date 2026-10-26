using Microsoft.EntityFrameworkCore;

namespace Shop.Api.Tenancy;

public sealed class PlatformContext : DbContext
{
    public PlatformContext(DbContextOptions<PlatformContext> options) : base(options) { }
    public DbSet<MerchantApplication> MerchantApplications => Set<MerchantApplication>();
    public DbSet<StoreRegistration> Stores => Set<StoreRegistration>();
    public DbSet<PlatformOperator> Operators => Set<PlatformOperator>();
    public DbSet<SubscriptionPlan> Plans => Set<SubscriptionPlan>();
    public DbSet<StoreSubscription> Subscriptions => Set<StoreSubscription>();
    public DbSet<SubscriptionEvent> SubscriptionEvents => Set<SubscriptionEvent>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<MerchantApplication>().HasIndex(a => a.Email).IsUnique();
        model.Entity<MerchantApplication>().Property(a => a.Version).IsConcurrencyToken();
        model.Entity<StoreRegistration>().HasIndex(s => s.Host).IsUnique();
        model.Entity<StoreRegistration>().HasIndex(s => s.ConnectionKey).IsUnique();
        model.Entity<StoreRegistration>().HasIndex(s => s.DatabaseIdentity).IsUnique();
        model.Entity<PlatformOperator>().HasIndex(s => s.Username).IsUnique();
        model.Entity<SubscriptionPlan>().HasIndex(p => p.Name).IsUnique();
        model.Entity<SubscriptionPlan>().Property(p => p.Version).IsConcurrencyToken();
        model.Entity<StoreSubscription>().HasKey(s => s.StoreId);
        model.Entity<StoreSubscription>().Property(s => s.Version).IsConcurrencyToken();
        model.Entity<StoreSubscription>().HasOne<StoreRegistration>().WithOne().HasForeignKey<StoreSubscription>(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<StoreSubscription>().HasOne<SubscriptionPlan>().WithMany().HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<SubscriptionEvent>().HasIndex(s => new { s.StoreId, s.CreatedAtUtc });
        foreach (var property in model.Model.GetEntityTypes().SelectMany(e => e.GetProperties()).Where(p => p.ClrType == typeof(DateTime)))
            property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc)));
    }
}

public sealed class StoreRegistration
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public string ConnectionKey { get; set; } = "";
    public string? DatabaseIdentity { get; set; }
    public string State { get; set; } = "Provisioning";
    public bool SubscriptionRequired { get; set; }
    public bool HasUsedTrial { get; set; }
}

public sealed class SubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string FeaturesJson { get; set; } = PlanFeatures.DefaultJson;
    public string Description { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string DescriptionAr { get; set; } = "";
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = "EGP";
    public int PeriodMonths { get; set; } = 1;
    public int TrialDays { get; set; }
    public bool AllowMerchantApplications { get; set; }
    public int? MaxProducts { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}

public sealed class StoreSubscription
{
    public string FeaturesJson { get; set; } = PlanFeatures.DefaultJson;
    public string StoreId { get; set; } = "";
    public int PlanId { get; set; }
    public string PlanName { get; set; } = "";
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = "";
    public int PeriodMonths { get; set; }
    public int? MaxProducts { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsTrial { get; set; }
    public bool IsCancelled { get; set; }
    public int Version { get; set; } = 1;
}

public sealed class SubscriptionEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string StoreId { get; set; } = "";
    public string Action { get; set; } = "";
    public int PlanId { get; set; }
    public string PlanName { get; set; } = "";
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = "";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ActorId { get; set; } = "";
    public string Reference { get; set; } = "";
}

public sealed class PlatformOperator
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public sealed class MerchantApplication
{
    public string FeaturesJson { get; set; } = PlanFeatures.DefaultJson;
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = "EGP";
    public int PeriodMonths { get; set; } = 1;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string StoreName { get; set; } = "";
    public string BusinessCategory { get; set; } = "";
    public string BusinessDescription { get; set; } = "";
    public string BusinessMode { get; set; } = "Commerce";
    public int PlanId { get; set; }
    public string PlanName { get; set; } = "";
    public int TrialDays { get; set; }
    public int? MaxProducts { get; set; }
    public string State { get; set; } = "Pending";
    public string ReviewNote { get; set; } = "";
    public string? StoreId { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public int Version { get; set; } = 1;
}
