using Shop.Application.Returns;
using Xunit;
namespace Shop.Application.Tests.Returns;

public class ReturnPolicyTests
{
    public static IEnumerable<object[]> Matrix() { foreach (var state in new[] { "Requested", "Approved", "Received", "Rejected", "Completed" }) foreach (var action in new[] { "Approve", "Reject", "Receive", "RecordRefund", "Unknown" }) yield return new object[] { state, action }; }
    [Theory]
    [MemberData(nameof(Matrix))]
    public void Only_existing_action_state_pairs_are_allowed(string state, string action)
    {
        string? expected = (state, action) switch { ("Requested", "Approve") => "Approved", ("Requested", "Reject") => "Rejected", ("Approved", "Reject") => "Rejected", ("Approved", "Receive") => "Received", ("Received", "RecordRefund") => "Completed", _ => null };
        if (expected is null) Assert.Throws<InvalidOperationException>(() => ReturnPolicy.Next(state, action, "note", false, 0, 10, ""));
        else Assert.Equal(expected, ReturnPolicy.Next(state, action, "note", false, 0, 10, ""));
    }
    [Fact] public void Refund_floor_and_shipping_exclusion_remain_exact() { Assert.Equal(26.66m, ReturnPolicy.MaximumRefund(90, 10, 100, 100, 1, 3)); Assert.Equal(0, ReturnPolicy.MaximumRefund(0, 10, 0, 100, 1, 3)); }
    [Theory]
    [InlineData(-1, "ref")]
    [InlineData(11, "ref")]
    [InlineData(1.001, "ref")]
    [InlineData(1, "")]
    public void Invalid_manual_refund_never_completes(decimal refund, string reference) => Assert.Throws<InvalidOperationException>(() => ReturnPolicy.Next("Received", "RecordRefund", "note", false, refund, 10, reference));
}
