namespace Shop.DAL.Models;

/// <summary>
/// A discount code that can be applied at checkout.
/// </summary>
public class Discount
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Unique discount code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>How <see cref="Value"/> is interpreted.</summary>
    public DiscountType Type { get; set; }

    /// <summary>Discount amount (percentage or fixed, per <see cref="Type"/>).</summary>
    public decimal Value { get; set; }

    /// <summary>Whether the discount is currently active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional UTC start of the validity window.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Optional UTC end of the validity window.</summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }
}
