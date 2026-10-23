using Shop.DAL.Models;

namespace Shop.BLL.IRepositories;

/// <summary>
/// Repository contract for <see cref="Review"/>, with the lookups used by the
/// product review display, a customer's own reviews, and purchase gating.
/// </summary>
public interface IReviewRepo : IGenericRepo<Review>
{
    /// <summary>Returns the reviews for a product (newest first), author included.</summary>
    Task<List<Review>> GetForProductAsync(int productId);

    /// <summary>Returns the reviews written by a customer, product included.</summary>
    Task<List<Review>> GetForCustomerAsync(int customerId);

    /// <summary>
    /// Whether the customer has purchased the product (an order item for the
    /// product exists on one of the customer's orders). Used to gate product
    /// reviews to verified purchasers.
    /// </summary>
    Task<bool> HasPurchasedAsync(int customerId, int productId);

    /// <summary>Whether the customer already reviewed this product.</summary>
    Task<bool> HasReviewedAsync(int customerId, int productId);
}
