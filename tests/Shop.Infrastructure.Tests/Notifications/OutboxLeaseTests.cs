using Microsoft.EntityFrameworkCore;
using Shop.Application.Notifications;
using Shop.Application.Common;
using Shop.Infrastructure.Notifications;
using Shop.DAL.Data;
using Xunit;
namespace Shop.Infrastructure.Tests.Notifications;

public class OutboxLeaseTests
{
    private static async Task<DbContextOptions<ShopContext>> Database()
    {
        var builder = new DbContextOptionsBuilder<ShopContext>(); var pg = Environment.GetEnvironmentVariable("SHOP_TEST_POSTGRES");
        if (SqlServerTestDatabase.Configure(builder)) { }
        else if (string.IsNullOrWhiteSpace(pg)) builder.UseSqlite("Data Source=" + Path.Combine(Path.GetTempPath(), "shop-refactor-" + Guid.NewGuid().ToString("N") + ".db"));
        else builder.UseNpgsql(new Npgsql.NpgsqlConnectionStringBuilder(pg) { Database = "shop_refactor_" + Guid.NewGuid().ToString("N") }.ConnectionString);
        await using var db = new ShopContext(builder.Options); if (db.Database.IsSqlServer()) { await db.Database.EnsureCreatedAsync(); SqlServerSchema.Apply(db, true); } else if (db.Database.IsNpgsql()) await db.Database.MigrateAsync(); else { await db.Database.EnsureCreatedAsync(); SqliteSchemaUpgrade.Apply(db, true); }
        return builder.Options;
    }
    [Fact]
    public async Task Two_workers_claiming_same_attempt_deliver_only_once()
    {
        var options = await Database(); var clock = new Clock(); OutboxEntry entry;
        await using (var db = new ShopContext(options)) { var q = Queue(db, clock); await q.EnqueueAsync("test@example.test", "subject", "private body"); entry = Assert.Single(await q.ReadAsync(true)); }
        var transport = new Transport();
        async Task Worker() { await using var db = new ShopContext(options); await Queue(db, clock).DeliverAsync(entry, transport); }
        await Task.WhenAll(Worker(), Worker()); Assert.Equal(1, transport.Calls);
        await using (var db = new ShopContext(options)) { var row = Assert.Single(await Queue(db, clock).ReadAsync()); Assert.Equal("Sent", row.State); Assert.Equal(1, row.Attempts); }
    }
    [Fact]
    public async Task Retries_are_bounded_and_expired_payload_is_not_delivered()
    {
        var options = await Database(); var clock = new Clock(); await using var db = new ShopContext(options); var queue = Queue(db, clock); var transport = new Transport { Fail = true };
        await queue.EnqueueAsync("test@example.test", "retry", "private body");
        for (int i = 0; i < 5; i++) { var row = Assert.Single(await queue.ReadAsync(true)); await queue.DeliverAsync(row, transport); clock.Now = clock.Now.AddHours(1); }
        var failed = Assert.Single(await queue.ReadAsync()); Assert.Equal("Failed", failed.State); Assert.Equal(5, failed.Attempts); Assert.Empty(await queue.ReadAsync(true));
        await queue.EnqueueAsync("other@example.test", "expiry", "private expired", expiresAtUtc: clock.Now.AddSeconds(-1)); Assert.Empty(await queue.ReadAsync(true));
        var expired = (await queue.ReadAsync()).Single(r => r.Subject == "expiry"); Assert.Equal("Expired", expired.State); Assert.Equal(5, transport.Calls);
    }
    private static SqlEmailQueue Queue(ShopContext db, Clock clock) => new(db, new EmailPayloadProtector(s => "test-encrypted:" + s, s => s[15..]), new Journal(), clock);
    private sealed class Clock : IClock { public DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc); public DateTime UtcNow => Now; }
    private sealed class Journal : IOutboxFailureJournal { public Task RecordFailureAsync(string id) => Task.CompletedTask; }
    private sealed class Transport : IEmailTransport
    {
        public int Calls; public bool Fail;
        public Task<string> DeliverAsync(string to, string subject, string body, string? file) { Interlocked.Increment(ref Calls); if (Fail) throw new InvalidOperationException("Injected failure"); Assert.Equal("private body", body); return Task.FromResult("Sent"); }
    }
}
