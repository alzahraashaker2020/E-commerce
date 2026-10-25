using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.Email;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.Data;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Admin;

/// <summary>
/// Admin orders endpoints: a paged list and a per-order status update.
/// JWT-protected under <c>api/admin/orders</c>.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly ShopContext _db;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminOrdersController> _logger;

    /// <summary>
    /// Creates a new <see cref="AdminOrdersController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    /// <param name="emailService">Service used to send the status-update email.</param>
    /// <param name="logger">Logger for best-effort email failures.</param>
    public AdminOrdersController(
        IRepoWrapper repo,
        IMapper mapper,
        IEmailService emailService,
        ILogger<AdminOrdersController> logger,
        ShopContext db)
    {
        _repo = repo;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
        _db = db;
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

        IQueryable<Order> query = _repo._Order.Query().Include(o => o.Customer).Include(o => o.StatusDefinition);

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
        await using var transaction = await _db.Database.BeginTransactionAsync();
        Order? order = await _db.Orders.Include(o => o.Items).Include(o => o.StatusDefinition).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound(ApiResponse<bool>.Fail("Not found", 404));
        }

        var definition = request.StatusId.HasValue
            ? await _db.OrderStatusDefinitions.FirstOrDefaultAsync(s => s.Id == request.StatusId && s.IsActive)
            : await _db.OrderStatusDefinitions.Where(s => s.Behavior == request.Status && s.IsActive).OrderBy(s => s.Id).FirstOrDefaultAsync();
        if (definition is null) return BadRequest(ApiResponse<bool>.Fail("Select an active order status.", 400));
        var nextStatus = definition.Behavior;
        if (order.StatusDefinitionId == definition.Id)
            return Ok(ApiResponse<bool>.Ok(true));
        if (order.Status == OrderStatus.Cancelled)
            return Conflict(ApiResponse<bool>.Fail("A cancelled order cannot be reopened. Create a new order instead.", 409));
        if (nextStatus == OrderStatus.Cancelled &&
            (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered))
            return Conflict(ApiResponse<bool>.Fail("Shipped or delivered orders require a return, not cancellation.", 409));

        bool needsStock = nextStatus is OrderStatus.Confirmed or OrderStatus.Processing
            or OrderStatus.ReadyToShip or OrderStatus.Shipped or OrderStatus.Delivered;
        bool deduct = needsStock && !order.StockDeducted;
        bool restore = nextStatus == OrderStatus.Cancelled && order.StockDeducted;
        try
        {
            if (deduct || restore)
            {
                foreach (var line in order.Items.GroupBy(i => new { i.ProductId, i.VariantId }))
                {
                    int quantity = checked(line.Sum(i => i.Quantity));
                    if (quantity <= 0)
                        return Conflict(ApiResponse<bool>.Fail("Order contains an invalid quantity.", 409));
                    int changed;
                    if (line.Key.VariantId.HasValue)
                    {
                        var query = _db.ProductVariants.Where(v => v.Id == line.Key.VariantId.Value && v.ProductId == line.Key.ProductId);
                        changed = deduct
                            ? await query.Where(v => v.IsActive && v.Product!.IsActive && v.StockQuantity >= quantity)
                                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - quantity))
                            : await query.ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + quantity));
                    }
                    else
                    {
                        var query = _db.Products.Where(p => p.Id == line.Key.ProductId && !p.HasVariants);
                        changed = deduct
                            ? await query.Where(p => p.IsActive && p.StockQuantity >= quantity)
                                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity))
                            : await query.ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity));
                    }
                    if (changed != 1)
                        return Conflict(ApiResponse<bool>.Fail("Insufficient stock or unavailable product/variant. The order status was not changed.", 409));
                }
                order.StockDeducted = deduct;
            }
            order.Status = nextStatus;
            order.StatusDefinitionId = definition.Id;
            order.StatusDefinition = definition;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ApiResponse<bool>.Fail("This order changed concurrently. Refresh and try again.", 409));
        }

        // Best-effort status-update email. Load the store settings for the
        // currency symbol (fall back to USD/$ if the single row is missing).
        // A failure here must never fail the status update.
        try
        {
            StoreSettings settings = await _repo._StoreSettings.Query().FirstOrDefaultAsync()
                ?? new StoreSettings { Id = 1, CurrencyCode = "USD", CurrencySymbol = "$" };
            await _emailService.SendStatusUpdateAsync(order, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send status-update email for order {OrderNumber}", order.OrderNumber);
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
