using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Authenticated customer account endpoints: profile view/update, order
/// history, and the customer's own reviews. JWT-protected and restricted to the
/// "Customer" role; the customer id is always taken from the token, never the
/// request body or route, so one customer can never read another's data.
/// </summary>
[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="AccountController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AccountController(IRepoWrapper repo, IMapper mapper)
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
    /// Returns the authenticated customer's profile.
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<CustomerProfileVM>>> GetProfile()
    {
        Customer? customer = await _repo._Customer.GetByIdAsync(CurrentCustomerId);
        if (customer is null)
        {
            return NotFound(ApiResponse<CustomerProfileVM>.Fail("Account not found", 404));
        }

        return Ok(ApiResponse<CustomerProfileVM>.Ok(_mapper.Map<CustomerProfileVM>(customer)));
    }

    /// <summary>
    /// Updates the authenticated customer's editable profile fields (name,
    /// phone, marketing consent). Email is read-only in this scaffold.
    /// </summary>
    /// <param name="request">The profile update payload.</param>
    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<CustomerProfileVM>>> UpdateProfile(
        [FromBody] CustomerProfileVM request)
    {
        Customer? customer = await _repo._Customer.GetByIdAsync(CurrentCustomerId);
        if (customer is null)
        {
            return NotFound(ApiResponse<CustomerProfileVM>.Fail("Account not found", 404));
        }

        // Only the editable fields are copied; email/credentials are untouched.
        customer.FullName = request.FullName.Trim();
        customer.Phone = request.Phone;
        customer.MarketingConsent = request.MarketingConsent;

        _repo._Customer.Update(customer);
        _repo.Save();

        return Ok(ApiResponse<CustomerProfileVM>.Ok(_mapper.Map<CustomerProfileVM>(customer)));
    }

    /// <summary>
    /// Returns the authenticated customer's order history (newest first), with
    /// line items and a string status.
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<List<OrderTrackingVM>>>> GetOrders()
    {
        List<Order> orders = await _repo._Order.Query()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == CurrentCustomerId)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<OrderTrackingVM>>.Ok(_mapper.Map<List<OrderTrackingVM>>(orders)));
    }

    /// <summary>
    /// Returns the reviews written by the authenticated customer.
    /// </summary>
    [HttpGet("reviews")]
    public async Task<ActionResult<ApiResponse<List<ReviewVM>>>> GetMyReviews()
    {
        List<Review> reviews = await _repo._Review.GetForCustomerAsync(CurrentCustomerId);
        return Ok(ApiResponse<List<ReviewVM>>.Ok(_mapper.Map<List<ReviewVM>>(reviews)));
    }
}
