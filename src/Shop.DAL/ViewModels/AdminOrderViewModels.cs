using System.ComponentModel.DataAnnotations;
using Shop.DAL.Models;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Order summary row for the admin orders list.
/// </summary>
public class AdminOrderListItemVM
{
    /// <summary>Order identifier.</summary>
    public int Id { get; set; }

    /// <summary>Order/tracking number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Customer or guest display name.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Current status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Grand total.</summary>
    public decimal Total { get; set; }

    /// <summary>UTC placement timestamp.</summary>
    public DateTime PlacedAt { get; set; }
}

/// <summary>
/// Request to change an order's status.
/// </summary>
public class UpdateOrderStatusRequest
{
    /// <summary>New order status (must be a defined value).</summary>
    [Required]
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; set; }
}
