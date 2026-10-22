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
/// Admin inventory endpoint. JWT-protected under <c>api/admin/inventory</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/inventory")]
public class AdminInventoryController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="AdminInventoryController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public AdminInventoryController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns every inventory row currently at/below its low-stock threshold
    /// (the per-row rule <c>QuantityOnHand &lt;= LowStockThreshold</c>).
    /// </summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<List<LowStockItemVM>>>> GetLowStock()
    {
        List<InventoryItem> lowStock = await _repo._InventoryItem.Query()
            .Include(i => i.Product)
            .Include(i => i.Variant)
            .Where(i => i.QuantityOnHand <= i.LowStockThreshold)
            .ToListAsync();

        return Ok(ApiResponse<List<LowStockItemVM>>.Ok(_mapper.Map<List<LowStockItemVM>>(lowStock)));
    }
}
