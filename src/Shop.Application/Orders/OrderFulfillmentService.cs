namespace Shop.Application.Orders;

public sealed class OrderFulfillmentService
{
    private readonly IOrderFulfillmentStore _store;
    public OrderFulfillmentService(IOrderFulfillmentStore store) => _store = store;
    public async Task<FulfillmentOutcome> ChangeStatusAsync(ChangeOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        await using var session = await _store.OpenAsync(cancellationToken);
        var order = await session.LoadOrderAsync(command.OrderId, cancellationToken);
        if (order is null) return new(FulfillmentOutcomeKind.NotFound, "Not found");
        var definition = await session.FindDefinitionAsync(command.DefinitionId, command.LegacyStatus, cancellationToken);
        if (definition is null) return new(FulfillmentOutcomeKind.Invalid, "Select an active order status.");
        var policy = OrderStatusTransitionPolicy.Decide(order.Status, definition.Behavior, order.DefinitionId == definition.Id, order.StockDeducted);
        if (policy.Error is not null) return new(FulfillmentOutcomeKind.Conflict, policy.Error);
        if (policy.NoChange) return new(FulfillmentOutcomeKind.Success);
        try
        {
            bool deducted = order.StockDeducted;
            if (policy.Deduct || policy.Restore)
            {
                foreach (var group in order.Lines.GroupBy(l => new { l.ProductId, l.VariantId }))
                {
                    int quantity = checked(group.Sum(l => l.Quantity));
                    if (quantity <= 0) return new(FulfillmentOutcomeKind.Conflict, "Order contains an invalid quantity.");
                    if (!await session.AdjustStockAsync(new(group.Key.ProductId, group.Key.VariantId, quantity), policy.Deduct, cancellationToken))
                        return new(FulfillmentOutcomeKind.Conflict, "Insufficient stock or unavailable product/variant. The order status was not changed.");
                }
                deducted = policy.Deduct;
            }
            await session.SaveAndQueueAsync(order, definition, deducted, cancellationToken);
            await session.CommitAsync(cancellationToken);
            return new(FulfillmentOutcomeKind.Success);
        }
        catch (FulfillmentConcurrencyException) { return new(FulfillmentOutcomeKind.Conflict, "This order changed concurrently. Refresh and try again."); }
    }
}
