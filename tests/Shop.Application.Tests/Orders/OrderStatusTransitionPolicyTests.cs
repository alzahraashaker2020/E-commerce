using Shop.Application.Orders;
using Xunit;
namespace Shop.Application.Tests.Orders;

public class OrderStatusTransitionPolicyTests
{
    public static IEnumerable<object[]> Cases() { foreach (var current in Enum.GetValues<FulfillmentStatus>()) foreach (var next in Enum.GetValues<FulfillmentStatus>()) foreach (bool same in new[] { false, true }) foreach (bool deducted in new[] { false, true }) yield return new object[] { current, next, same, deducted }; }
    [Theory]
    [MemberData(nameof(Cases))]
    public void Existing_transition_precedence_is_preserved(FulfillmentStatus current, FulfillmentStatus next, bool same, bool deducted)
    {
        var d = OrderStatusTransitionPolicy.Decide(current, next, same, deducted);
        string? error = current == FulfillmentStatus.Delivered && next != FulfillmentStatus.Delivered ? "A delivered order requires a return; its fulfillment status cannot be reopened." : same ? null : current == FulfillmentStatus.Cancelled ? "A cancelled order cannot be reopened. Create a new order instead." : next == FulfillmentStatus.Cancelled && current == FulfillmentStatus.Shipped ? "Shipped or delivered orders require a return, not cancellation." : null;
        Assert.Equal(error, d.Error); Assert.Equal(error is null && same, d.NoChange);
        bool applies = error is null && !same;
        Assert.Equal(applies && !deducted && new[] { FulfillmentStatus.Confirmed, FulfillmentStatus.Processing, FulfillmentStatus.ReadyToShip, FulfillmentStatus.Shipped, FulfillmentStatus.Delivered }.Contains(next), d.Deduct);
        Assert.Equal(applies && deducted && next == FulfillmentStatus.Cancelled, d.Restore);
    }
}
