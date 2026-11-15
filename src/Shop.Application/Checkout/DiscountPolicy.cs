namespace Shop.Application.Checkout;

public sealed record DiscountSnapshot(bool Active, DateTime? StartsAt, DateTime? EndsAt, bool Percentage, decimal Value);
public static class DiscountPolicy
{
    public static (decimal Amount, string? Message) Calculate(DiscountSnapshot? discount, decimal subtotal, DateTime now)
    {
        if (discount is null) return (0m, "Discount code not applied: unknown code");
        if (!discount.Active) return (0m, "Discount code not applied: inactive code");
        if ((discount.StartsAt.HasValue && now < discount.StartsAt.Value) || (discount.EndsAt.HasValue && now > discount.EndsAt.Value))
            return (0m, "Discount code not applied: expired code");
        decimal amount = discount.Percentage ? subtotal * (discount.Value / 100m) : discount.Value;
        if (amount > subtotal) amount = subtotal;
        return (amount, null);
    }
}
