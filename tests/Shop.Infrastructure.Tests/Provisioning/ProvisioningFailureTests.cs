using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Api.Tenancy;
using Shop.Application.Common;
using Shop.Application.Provisioning;
using Shop.Infrastructure.Provisioning;
using Shop.BLL.Security;
using Xunit;
namespace Shop.Infrastructure.Tests.Provisioning;

public class ProvisioningFailureTests
{
    [Fact]
    public async Task Existing_database_file_is_never_adopted_or_changed()
    {
        var directory = Path.Combine(Path.GetTempPath(), "shop-provision-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var tenant = Path.Combine(directory, "existing.db"); await File.WriteAllTextAsync(tenant, "EXISTING PRIVATE TENANT DATA");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "StoreConnections:Fresh:Provider", "Sqlite" }, { "StoreConnections:Fresh:ConnectionString", "Data Source=" + tenant } }).Build();
        await using var platform = new PlatformContext(new DbContextOptionsBuilder<PlatformContext>().UseSqlite("Data Source=" + Path.Combine(directory, "registry.db")).Options); await platform.Database.EnsureCreatedAsync();
        await using var adapter = new PlatformRegistrationStore(platform, config, new Passwords(), new Clock());
        var outcome = await new StoreProvisioningService(adapter).CreateAsync(new() { Host = "new.localhost", Name = "New", ConnectionKey = "Fresh", OwnerUsername = "owner", OwnerEmail = "test@example.test", OwnerPassword = "test-password" }, "operator");
        Assert.Equal(ProvisioningOutcomeKind.Conflict, outcome.Kind); Assert.Equal("Provisioning requires a new, empty database path.", outcome.Message); Assert.Empty(await platform.Stores.ToListAsync()); Assert.Equal("EXISTING PRIVATE TENANT DATA", await File.ReadAllTextAsync(tenant));
    }
    private sealed class Clock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
    private sealed class Passwords : IPasswordHasherService { public string Hash(string value) => "test-hash"; public bool Verify(string hash, string value) => false; }
}
