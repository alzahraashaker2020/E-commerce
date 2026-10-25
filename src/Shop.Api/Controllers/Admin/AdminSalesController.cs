using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Admin;

/// <summary>
/// Admin sales overview / revenue summary endpoint. JWT-protected under
/// <c>api/admin/sales</c>.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/sales")]
public class AdminSalesController : ControllerBase
{
    private const int TopProductCount = 5;

    private readonly IRepoWrapper _repo;

    /// <summary>
    /// Creates a new <see cref="AdminSalesController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    public AdminSalesController(IRepoWrapper repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Returns a sales overview for the optional date window: total revenue,
    /// order count, average order value, current low-stock count, and the top
    /// products by units sold within the window.
    /// </summary>
    /// <param name="from">Optional inclusive UTC start of the window.</param>
    /// <param name="to">Optional inclusive UTC end of the window.</param>
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<SalesOverviewVM>>> GetOverview(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        IQueryable<Order> orders = _repo._Order.Query().Include(o => o.Items);

        if (from.HasValue)
        {
            orders = orders.Where(o => o.PlacedAt >= from.Value);
        }

        if (to.HasValue)
        {
            orders = orders.Where(o => o.PlacedAt <= to.Value);
        }

        string currencyCode = (await _repo._StoreSettings.Query().FirstAsync()).CurrencyCode;
        List<Order> windowOrders = await orders.Where(o => o.CurrencyCode == currencyCode).ToListAsync();

        decimal totalRevenue = windowOrders.Sum(o => o.Total);
        int orderCount = windowOrders.Count;
        decimal averageOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0m;

        // Low-stock is a current read-side rule, independent of the window:
        // count no-variant products plus variants at/below their threshold.
        int productLowStockCount = await _repo._Product.Query()
            .CountAsync(p => !p.HasVariants && p.StockQuantity <= p.LowStockThreshold);
        int variantLowStockCount = await _repo._Product.Query()
            .SelectMany(p => p.Variants)
            .CountAsync(v => v.StockQuantity <= v.LowStockThreshold);
        int lowStockCount = productLowStockCount + variantLowStockCount;

        // Top products by units sold within the window.
        List<TopProductVM> topProducts = windowOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductNameSnapshot })
            .Select(g => new TopProductVM
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductNameSnapshot,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(t => t.QuantitySold)
            .Take(TopProductCount)
            .ToList();

        var overview = new SalesOverviewVM
        {
            CurrencyCode = currencyCode,
            TotalRevenue = totalRevenue,
            ShippingRevenue = windowOrders.Sum(o => o.ShippingTotal),
            ProductRevenue = windowOrders.Sum(o => o.Subtotal - o.DiscountTotal),
            OrderCount = orderCount,
            AverageOrderValue = averageOrderValue,
            LowStockCount = lowStockCount,
            TopProducts = topProducts
        };

        return Ok(ApiResponse<SalesOverviewVM>.Ok(overview));
    }
}
