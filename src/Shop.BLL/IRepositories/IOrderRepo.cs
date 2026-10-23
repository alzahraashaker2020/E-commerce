using Shop.DAL.Models;

namespace Shop.BLL.IRepositories;

/// <summary>
/// Repository contract for <see cref="Order"/>, adding order-number lookup.
/// </summary>
public interface IOrderRepo : IGenericRepo<Order>
{
    /// <summary>
    /// Loads an order by its human-readable order number, including its line
    /// items, or <c>null</c> when no order matches.
    /// </summary>
    /// <param name="orderNumber">The unique order/tracking number.</param>
    Task<Order?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>
    /// Loads the most recently placed order for the supplied guest email
    /// (case-insensitive), including its line items, or <c>null</c> when no
    /// order matches.
    /// </summary>
    /// <param name="email">The guest email used at checkout.</param>
    Task<Order?> GetMostRecentByGuestEmailAsync(string email);
}
