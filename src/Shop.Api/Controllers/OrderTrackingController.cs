using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Public first-party order tracking endpoint: look up an order by its
/// human-readable order number.
/// </summary>
[ApiController]
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
        Order? order = await _repo._Order.GetByOrderNumberAsync(orderNumber);
        if (order is null)
        {
            return NotFound(ApiResponse<OrderTrackingVM>.Fail("Order not found", 404));
        }

        return Ok(ApiResponse<OrderTrackingVM>.Ok(_mapper.Map<OrderTrackingVM>(order)));
    }
}
