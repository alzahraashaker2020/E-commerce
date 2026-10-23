namespace Shop.DAL.Models;

/// <summary>
/// A customer review. A review always belongs to a customer and targets a
/// product; it may optionally reference the order the product was purchased on
/// (an "order review"). Product reviews are gated server-side to customers who
/// actually purchased the product.
/// </summary>
public class Review
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The customer who wrote the review.</summary>
    public int CustomerId { get; set; }

    /// <summary>Navigation to the author.</summary>
    public Customer? Customer { get; set; }

    /// <summary>The product being reviewed.</summary>
    public int ProductId { get; set; }

    /// <summary>Navigation to the reviewed product.</summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Optional order this review is associated with (set for order reviews, or
    /// to record the purchase that authorises a product review).
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>Navigation to the associated order, when set.</summary>
    public Order? Order { get; set; }

    /// <summary>Star rating, 1-5.</summary>
    public int Rating { get; set; }

    /// <summary>Optional free-text comment.</summary>
    public string? Comment { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }
}
