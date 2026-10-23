using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Order"/>, implementing order-number lookup.
/// </summary>
public class OrderRepo : GenericRepo<Order>, IOrderRepo
{
    /// <summary>
    /// Creates a new <see cref="OrderRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public OrderRepo(ShopContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        // Include the order's line items so tracking can show a line summary.
        return _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    /// <inheritdoc />
    public Task<Order?> GetMostRecentByGuestEmailAsync(string email)
    {
        // Normalise both sides so the guest email match is case-insensitive,
        // then take the newest matching order (with its line items).
        string normalized = email.ToLower();
        return _context.Orders
            .Include(o => o.Items)
            .Where(o => o.GuestEmail != null && o.GuestEmail.ToLower() == normalized)
            .OrderByDescending(o => o.PlacedAt)
            .FirstOrDefaultAsync();
    }
}
