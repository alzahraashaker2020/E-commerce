namespace Shop.DAL.Models;

/// <summary>
/// How a <see cref="Discount"/>'s value is interpreted. Persisted as its
/// backing <see cref="int"/> value.
/// </summary>
public enum DiscountType
{
    /// <summary>Value is a percentage (0-100) off the subtotal.</summary>
    Percentage = 0,

    /// <summary>Value is a fixed monetary amount off the subtotal.</summary>
    Fixed = 1
}
