using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;
using System.Security.Claims;
using Shop.Api.Tenancy;

namespace Shop.Api.Controllers;

/// <summary>
/// Public first-party order tracking endpoint: look up an order by its
/// human-readable order number.
/// </summary>
[ApiController]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("Tracking")]
[Route("api/[controller]")]
public class OrderTrackingController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="OrderTrackingController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public OrderTrackingController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns the tracking view (status + line summary) for the supplied order
    /// number, or a 404 failure envelope when the order is unknown.
    /// </summary>
    /// <param name="orderNumber">The unique order/tracking number.</param>
    [HttpGet("{orderNumber}")]
    public async Task<ActionResult<ApiResponse<OrderTrackingVM>>> Track(string orderNumber)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var access = HttpContext.RequestServices.GetRequiredService<OrderTrackingAccess>();
        bool proof = access.Verify(orderNumber, Request.Headers["X-Order-Tracking-Token"].FirstOrDefault());
        int? customerId = User.IsInRole("Customer") && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;
        if (!proof && customerId is null) return NotFound(ApiResponse<OrderTrackingVM>.Fail("Order not found or access unavailable",404));
        Order? order = await _repo._Order.GetByOrderNumberAsync(orderNumber);
        if (order is null || (!proof && order.CustomerId != customerId))
        {
            return NotFound(ApiResponse<OrderTrackingVM>.Fail("Order not found or access unavailable", 404));
        }

        return Ok(ApiResponse<OrderTrackingVM>.Ok(_mapper.Map<OrderTrackingVM>(order)));
    }

    /// <summary>
    /// Returns the tracking view for the most recent order placed with the
    /// supplied guest email, or a 404 failure envelope when none is found.
    /// </summary>
    /// <param name="email">The guest email used at checkout.</param>
    [HttpGet("by-email")]
    public ActionResult<ApiResponse<OrderTrackingVM>> TrackByEmail()
    {
        Response.Headers.CacheControl = "no-store";
        return NotFound(ApiResponse<OrderTrackingVM>.Fail("Track using your account or order number and private tracking code.", 404));
    }
}
