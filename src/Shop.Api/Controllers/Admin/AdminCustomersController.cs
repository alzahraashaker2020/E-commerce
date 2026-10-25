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
/// Admin customers endpoint: a paged customers list. JWT-protected under
/// <c>api/admin/customers</c>.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/customers")]
public class AdminCustomersController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="AdminCustomersController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AdminCustomersController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns a page of customers with the total count. Paging is clamped via
    /// the shared helper and the total count is captured before Skip/Take.
    /// </summary>
    /// <param name="page">1-based page number (clamped to &gt;= 1).</param>
    /// <param name="pageSize">Page size (clamped to 1-100, default 12).</param>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultVM<CustomerListItemVM>>>> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        (int clampedPage, int clampedPageSize) = PagingHelper.Clamp(page, pageSize);

        IQueryable<Customer> query = _repo._Customer.Query();

        // Count before paging so the admin list keeps a stable total.
        int totalCount = await query.CountAsync();

        List<Customer> customers = await query
            .OrderBy(c => c.Id)
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync();

        var result = new PagedResultVM<CustomerListItemVM>
        {
            Items = _mapper.Map<List<CustomerListItemVM>>(customers),
            TotalCount = totalCount,
            Page = clampedPage,
            PageSize = clampedPageSize
        };

        return Ok(ApiResponse<PagedResultVM<CustomerListItemVM>>.Ok(result));
    }
}
