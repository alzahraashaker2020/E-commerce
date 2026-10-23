using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Review"/>, implementing the product/customer
/// review queries and the purchase/duplicate gating checks.
/// </summary>
public class ReviewRepo : GenericRepo<Review>, IReviewRepo
{
    /// <summary>
    /// Creates a new <see cref="ReviewRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public ReviewRepo(ShopContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<List<Review>> GetForProductAsync(int productId)
    {
        // Newest first, with the author so the VM can show a display name.
        return _context.Reviews
            .Include(r => r.Customer)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public Task<List<Review>> GetForCustomerAsync(int customerId)
    {
        return _context.Reviews
            .Include(r => r.Product)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public Task<bool> HasPurchasedAsync(int customerId, int productId)
    {
        // True when an order item for the product exists on one of the
        // customer's own (non-guest) orders.
        return _context.OrderItems
            .AnyAsync(oi => oi.ProductId == productId
                && oi.Order != null
                && oi.Order.CustomerId == customerId);
    }

    /// <inheritdoc />
    public Task<bool> HasReviewedAsync(int customerId, int productId)
    {
        return _context.Reviews
            .AnyAsync(r => r.CustomerId == customerId && r.ProductId == productId);
    }
}
