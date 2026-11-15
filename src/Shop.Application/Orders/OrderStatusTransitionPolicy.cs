namespace Shop.Application.Orders;

// Stable application vocabulary; persistence enum values are translated at the boundary.
public enum FulfillmentStatus { Pending, Paid, Processing, Shipped, Delivered, Cancelled, Confirmed, ReadyToShip }
public sealed record TransitionDecision(string? Error, bool NoChange, bool Deduct, bool Restore);
public static class OrderStatusTransitionPolicy
{
    public static TransitionDecision Decide(FulfillmentStatus current, FulfillmentStatus next, bool sameDefinition, bool stockDeducted)
    {
        if (current == FulfillmentStatus.Delivered && next != FulfillmentStatus.Delivered)
            return new("A delivered order requires a return; its fulfillment status cannot be reopened.", false, false, false);
        if (sameDefinition) return new(null, true, false, false);
        if (current == FulfillmentStatus.Cancelled)
            return new("A cancelled order cannot be reopened. Create a new order instead.", false, false, false);
        if (next == FulfillmentStatus.Cancelled && current is FulfillmentStatus.Shipped or FulfillmentStatus.Delivered)
            return new("Shipped or delivered orders require a return, not cancellation.", false, false, false);
        bool needsStock = next is FulfillmentStatus.Confirmed or FulfillmentStatus.Processing or FulfillmentStatus.ReadyToShip or FulfillmentStatus.Shipped or FulfillmentStatus.Delivered;
        return new(null, false, needsStock && !stockDeducted, next == FulfillmentStatus.Cancelled && stockDeducted);
    }
}
