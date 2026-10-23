using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Product reviews. Reading a product's reviews is public; writing a review
/// requires an authenticated customer who has purchased the product (verified
/// purchaser gating) and has not already reviewed it.
/// </summary>
[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="ReviewsController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public ReviewsController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Resolves the authenticated customer id from the JWT subject claim.
    /// </summary>
    private int CurrentCustomerId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("nameid")
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? "0");

    /// <summary>
    /// Public: returns the review summary (average + count + list) for a
    /// product.
    /// </summary>
    /// <param name="productId">The product id.</param>
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<ApiResponse<ProductReviewSummaryVM>>> GetForProduct(int productId)
    {
        List<Review> reviews = await _repo._Review.GetForProductAsync(productId);

        var summary = new ProductReviewSummaryVM
        {
            ProductId = productId,
            ReviewCount = reviews.Count,
            AverageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 2) : 0,
            Reviews = _mapper.Map<List<ReviewVM>>(reviews)
        };

        return Ok(ApiResponse<ProductReviewSummaryVM>.Ok(summary));
    }

    /// <summary>
    /// Customer-only: creates a product review. Gated so the customer must have
    /// purchased the product and must not have already reviewed it.
    /// </summary>
    /// <param name="request">The review payload.</param>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReviewVM>>> Create([FromBody] CreateReviewRequest request)
    {
        int customerId = CurrentCustomerId;

        // Gate 1: the product must exist.
        Product? product = await _repo._Product.GetByIdAsync(request.ProductId);
        if (product is null)
        {
            return NotFound(ApiResponse<ReviewVM>.Fail("Product not found", 404));
        }

        // Gate 2: verified-purchaser requirement.
        if (!await _repo._Review.HasPurchasedAsync(customerId, request.ProductId))
        {
            return StatusCode(403, ApiResponse<ReviewVM>.Fail(
                "You can only review products you have purchased", 403));
        }

        // Gate 3: one review per customer per product.
        if (await _repo._Review.HasReviewedAsync(customerId, request.ProductId))
        {
            return Conflict(ApiResponse<ReviewVM>.Fail("You have already reviewed this product", 409));
        }

        var review = new Review
        {
            CustomerId = customerId,
            ProductId = request.ProductId,
            OrderId = request.OrderId,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow,
            IsDemoData = false
        };

        _repo._Review.Add(review);
        _repo.Save();

        // Reload with author/product so the response VM is fully populated.
        Review created = (await _repo._Review.GetForProductAsync(request.ProductId))
            .First(r => r.Id == review.Id);

        return Ok(ApiResponse<ReviewVM>.Ok(_mapper.Map<ReviewVM>(created)));
    }
}
