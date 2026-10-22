using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Admin;

/// <summary>
/// Admin orders endpoints: a paged list and a per-order status update.
/// JWT-protected under <c>api/admin/orders</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="AdminOrdersController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AdminOrdersController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns a page of orders with the total count. Paging is clamped via the
    /// shared helper and the total count is captured before Skip/Take.
    /// </summary>
    /// <param name="page">1-based page number (clamped to &gt;= 1).</param>
    /// <param name="pageSize">Page size (clamped to 1-100, default 12).</param>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultVM<AdminOrderListItemVM>>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        (int clampedPage, int clampedPageSize) = PagingHelper.Clamp(page, pageSize);

        IQueryable<Order> query = _repo._Order.Query().Include(o => o.Customer);

        // Count before paging so the admin list keeps a stable total.
        int totalCount = await query.CountAsync();

        List<Order> orders = await query
            .OrderByDescending(o => o.PlacedAt)
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync();

        var result = new PagedResultVM<AdminOrderListItemVM>
        {
            Items = _mapper.Map<List<AdminOrderListItemVM>>(orders),
            TotalCount = totalCount,
            Page = clampedPage,
            PageSize = clampedPageSize
        };

        return Ok(ApiResponse<PagedResultVM<AdminOrderListItemVM>>.Ok(result));
    }

    /// <summary>
    /// Updates the status of a single order. Returns 404 when the order does
    /// not exist.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="request">The new status.</param>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request)
    {
        Order? order = await _repo._Order.GetByIdAsync(id);
        if (order is null)
        {
            return NotFound(ApiResponse<bool>.Fail("Not found", 404));
        }

        order.Status = request.Status;
        _repo._Order.Update(order);
        _repo.Save();

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
