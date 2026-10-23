namespace Shop.DAL.Models;

/// <summary>
/// Lifecycle states an <see cref="Order"/> moves through, from creation to
/// fulfilment. Persisted as its backing <see cref="int"/> value.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order created but not yet paid.</summary>
    Pending = 0,

    /// <summary>Payment captured.</summary>
    Paid = 1,

    /// <summary>Order is being prepared for shipment.</summary>
    Processing = 2,

    /// <summary>Order has been handed to the carrier.</summary>
    Shipped = 3,

    /// <summary>Order has reached the customer.</summary>
    Delivered = 4,

    /// <summary>Order was cancelled.</summary>
    Cancelled = 5,

    /// <summary>Order has been confirmed by the merchant.</summary>
    Confirmed = 6,

    /// <summary>Order is packed and ready to hand to the carrier.</summary>
    ReadyToShip = 7
}
