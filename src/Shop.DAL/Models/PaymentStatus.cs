namespace Shop.DAL.Models;

/// <summary>
/// Settlement state of an <see cref="Order"/>'s payment. Persisted as its
/// backing <see cref="int"/> value.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Payment has not been captured yet.</summary>
    Pending = 0,

    /// <summary>Payment has been captured in full.</summary>
    Paid = 1,

    /// <summary>Amount is collected on delivery (cash).</summary>
    CashOnDelivery = 2
}
