using Shop.Application.Checkout;
using Shop.Application.Common;
using Xunit;
namespace Shop.Application.Tests.Checkout;

public class CheckoutServiceTests
{
    [Fact]
    public async Task Receipt_replay_does_not_reprice_or_open_new_transaction()
    {
        var store = new Store { Receipt = new("guest", "original", "saved-json") };
        var result = await new CheckoutService(store, new Boundary(), new Clock()).PlaceAsync(new() { IdempotencyKey = Guid.NewGuid() }, new(false, false, false, null), "original");
        Assert.Equal("saved-json", result.ReplayJson); Assert.False(store.Begun); Assert.False(store.ProductRead); Assert.True(store.Disposed);
    }
    [Fact]
    public async Task Changed_receipt_details_conflict_without_repricing()
    {
        var store = new Store { Receipt = new("guest", "original", "saved-json") };
        var result = await new CheckoutService(store, new Boundary(), new Clock()).PlaceAsync(new() { IdempotencyKey = Guid.NewGuid() }, new(false, false, false, null), "changed");
        Assert.Equal(CheckoutOutcomeKind.Conflict, result.Kind); Assert.False(result.NotCreated); Assert.False(store.ProductRead);
    }
    [Fact]
    public async Task Quote_and_place_preserve_different_missing_variant_outcomes()
    {
        var store = new Store(); var service = new CheckoutService(store, new Boundary(), new Clock());
        var line = new CheckoutLine { ProductId = 1, VariantId = 999, Quantity = 1 };
        var quote = await service.QuoteAsync(new() { AreaId = 1, Lines = new() { line } });
        var placed = await service.PlaceAsync(new() { IdempotencyKey = Guid.NewGuid(), AreaId = 1, Lines = new() { line } }, new(false, false, false, null), "hash");
        Assert.Equal(CheckoutOutcomeKind.Conflict, quote.Kind); Assert.Equal(CheckoutOutcomeKind.Invalid, placed.Kind); Assert.True(placed.NotCreated);
    }
    [Fact]
    public async Task Invalid_session_never_falls_back_to_guest()
    {
        var store = new Store(); var result = await new CheckoutService(store, new Boundary(), new Clock()).PlaceAsync(new() { IdempotencyKey = Guid.NewGuid() }, new(true, false, false, null), "hash");
        Assert.Equal(CheckoutOutcomeKind.Unauthenticated, result.Kind); Assert.False(store.Opened);
    }
    private sealed class Clock : IClock { public DateTime UtcNow => new(2026, 1, 1); }
    private sealed class Boundary : ICheckoutBoundary
    {
        public bool DiscountsAllowed => true; public string CreateTrackingProof(string n) => "proof"; public string NewOrderNumber(DateTime n) => "test"; public string EncodeReceipt(CheckoutConfirmation c, string? m) => "receipt";
    }
    private sealed class Store : ICheckoutStore, ICheckoutSession
    {
        public ReceiptSnapshot? Receipt; public bool Begun, ProductRead, Disposed, Opened;
        public Task<CheckoutCustomer?> CustomerAsync(int id, CancellationToken ct) => Task.FromResult<CheckoutCustomer?>(null);
        public Task<ICheckoutSession> OpenAsync(CancellationToken ct) { Opened = true; return Task.FromResult<ICheckoutSession>(this); }
        public Task<CheckoutProduct?> ProductAsync(int id, bool active, CancellationToken ct) { ProductRead = true; return Task.FromResult<CheckoutProduct?>(new(1, "Product", true, true, 10, 10, Array.Empty<CheckoutVariant>())); }
        public Task<CheckoutSettings?> SettingsAsync(bool required, CancellationToken ct) => Task.FromResult<CheckoutSettings?>(new("USD", "$"));
        public Task<CheckoutArea?> AreaAsync(int id, string currency, CancellationToken ct) => Task.FromResult<CheckoutArea?>(new(1, "Area", 1, new("Region")));
        public Task<DiscountSnapshot?> DiscountAsync(string code, CancellationToken ct) => Task.FromResult<DiscountSnapshot?>(null);
        public Task<InitialStatus> InitialStatusAsync(CancellationToken ct) => Task.FromResult(new InitialStatus(1, "Pending"));
        public Task SaveOrderAsync(CheckoutOrder order, CancellationToken ct) => Task.CompletedTask;
        public Task QueueSummaryAsync(string number, CancellationToken ct) => Task.CompletedTask;
        public Task<ReceiptSnapshot?> ReceiptAsync(string key, CancellationToken ct) => Task.FromResult(Receipt);
        public Task BeginAsync(CancellationToken ct) { Begun = true; return Task.CompletedTask; }
        public Task WriteReceiptAsync(string k, string o, string h, string j, CancellationToken ct) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken ct) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct) => Task.CompletedTask;
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }
}
