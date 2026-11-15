namespace Shop.Application.Orders;

public interface IOrderFulfillmentStore
{
    Task<IOrderFulfillmentSession> OpenAsync(CancellationToken cancellationToken);
}
public interface IOrderFulfillmentSession : IAsyncDisposable
{
    Task<FulfillmentOrder?> LoadOrderAsync(int id, CancellationToken cancellationToken);
    Task<FulfillmentDefinition?> FindDefinitionAsync(int? id, FulfillmentStatus? legacy, CancellationToken cancellationToken);
    Task<bool> AdjustStockAsync(FulfillmentLine line, bool deduct, CancellationToken cancellationToken);
    Task SaveAndQueueAsync(FulfillmentOrder order, FulfillmentDefinition definition, bool stockDeducted, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}
