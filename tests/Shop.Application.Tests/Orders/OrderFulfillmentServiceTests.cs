using Shop.Application.Orders;
using Xunit;
namespace Shop.Application.Tests.Orders;

public class OrderFulfillmentServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Conflict_or_enqueue_failure_never_commits(bool enqueueFailure)
    {
        var store = new FakeSession { FailQueue = enqueueFailure, StockAvailable = enqueueFailure };
        var service = new OrderFulfillmentService(store);
        if (enqueueFailure) await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangeStatusAsync(new(1, 2, null)));
        else Assert.Equal(FulfillmentOutcomeKind.Conflict, (await service.ChangeStatusAsync(new(1, 2, null))).Kind);
        Assert.False(store.Committed); Assert.True(store.Disposed);
    }
    [Fact]
    public async Task Successful_transition_commits_once_and_groups_lines()
    {
        var store = new FakeSession(); var result = await new OrderFulfillmentService(store).ChangeStatusAsync(new(1, 2, null));
        Assert.Equal(FulfillmentOutcomeKind.Success, result.Kind); Assert.True(store.Committed); Assert.Single(store.Adjustments); Assert.Equal(3, store.Adjustments[0].Quantity);
    }
    private sealed class FakeSession : IOrderFulfillmentStore, IOrderFulfillmentSession
    {
        public bool FailQueue, StockAvailable = true, Committed, Disposed; public List<FulfillmentLine> Adjustments = new();
        public Task<IOrderFulfillmentSession> OpenAsync(CancellationToken ct) => Task.FromResult<IOrderFulfillmentSession>(this);
        public Task<FulfillmentOrder?> LoadOrderAsync(int id, CancellationToken ct) => Task.FromResult<FulfillmentOrder?>(new(1, FulfillmentStatus.Pending, 1, false, new[] { new FulfillmentLine(1, null, 1), new FulfillmentLine(1, null, 2) }));
        public Task<FulfillmentDefinition?> FindDefinitionAsync(int? id, FulfillmentStatus? status, CancellationToken ct) => Task.FromResult<FulfillmentDefinition?>(new(2, FulfillmentStatus.Confirmed));
        public Task<bool> AdjustStockAsync(FulfillmentLine line, bool deduct, CancellationToken ct) { Adjustments.Add(line); return Task.FromResult(StockAvailable); }
        public Task SaveAndQueueAsync(FulfillmentOrder order, FulfillmentDefinition definition, bool deducted, CancellationToken ct) => FailQueue ? Task.FromException(new InvalidOperationException("Injected enqueue failure")) : Task.CompletedTask;
        public Task CommitAsync(CancellationToken ct) { Committed = true; return Task.CompletedTask; }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }
}
