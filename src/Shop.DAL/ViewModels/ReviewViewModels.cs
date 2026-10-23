using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Request to create a product review. The customer is taken from the auth
/// token, never the body. Product reviews are gated server-side to customers
/// who purchased the product.
/// </summary>
public class CreateReviewRequest
{
    /// <summary>Product being reviewed (required).</summary>
    [Required]
    public int ProductId { get; set; }

    /// <summary>Optional order the review relates to.</summary>
    public int? OrderId { get; set; }

    /// <summary>Star rating, 1-5.</summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>Optional free-text comment (max 2000 chars).</summary>
    [StringLength(2000)]
    public string? Comment { get; set; }
}

/// <summary>
/// A review as shown on a product page or in a customer's review list.
/// </summary>
public class ReviewVM
{
    /// <summary>Review id.</summary>
    public int Id { get; set; }

    /// <summary>Reviewed product id.</summary>
    public int ProductId { get; set; }

    /// <summary>Reviewed product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Author display name.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Star rating, 1-5.</summary>
    public int Rating { get; set; }

    /// <summary>Optional comment.</summary>
    public string? Comment { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Aggregate review summary for a product (average + count).
/// </summary>
public class ProductReviewSummaryVM
{
    /// <summary>Product id.</summary>
    public int ProductId { get; set; }

    /// <summary>Average rating (0 when no reviews).</summary>
    public double AverageRating { get; set; }

    /// <summary>Number of reviews.</summary>
    public int ReviewCount { get; set; }

    /// <summary>The individual reviews.</summary>
    public List<ReviewVM> Reviews { get; set; } = new List<ReviewVM>();
}
