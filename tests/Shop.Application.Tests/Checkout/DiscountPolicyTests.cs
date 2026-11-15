using Shop.Application.Checkout;
using Xunit;
namespace Shop.Application.Tests.Checkout;

public class DiscountPolicyTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    [Fact] public void Invalid_codes_are_skipped_not_rejected() { Assert.Equal("Discount code not applied: unknown code", DiscountPolicy.Calculate(null, 100, Now).Message); Assert.Equal(0m, DiscountPolicy.Calculate(new(false, null, null, true, 10), 100, Now).Amount); }
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public void Start_and_end_are_inclusive(int offset, int expected) { Assert.Equal(expected, DiscountPolicy.Calculate(new(true, Now, Now, true, 10), 100, Now.AddTicks(offset)).Amount); }
    [Theory]
    [InlineData(true, 25, 80, 20)]
    [InlineData(false, 25, 80, 25)]
    [InlineData(false, 100, 80, 80)]
    public void Percentage_fixed_and_cap_preserve_decimal_math(bool percentage, int value, int subtotal, int expected) => Assert.Equal(expected, DiscountPolicy.Calculate(new(true, null, null, percentage, value), subtotal, Now).Amount);
}
