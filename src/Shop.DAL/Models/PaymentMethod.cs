namespace Shop.DAL.Models;

/// <summary>
/// How an <see cref="Order"/> is paid for. Persisted as its backing
/// <see cref="int"/> value.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Cash on delivery.</summary>
    Cash = 0,

    /// <summary>Card (e.g. Visa) payment.</summary>
    Card = 1
}
