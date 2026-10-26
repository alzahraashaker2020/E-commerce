using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.Email;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.Data;
using Shop.DAL.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;

namespace Shop.Api.Controllers;

/// <summary>
/// Customer and guest checkout with durable, owner-bound retry receipts.
/// Totals are computed server-side from trusted prices.
/// An unknown/inactive/expired discount code never fails the order —
/// it is placed without the discount and the response explains why.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly ShopContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<CheckoutController> _logger;

    /// <summary>
    /// Creates a new <see cref="CheckoutController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="emailService">Service used to send the order-summary email.</param>
    /// <param name="logger">Logger for best-effort email failures.</param>
    public CheckoutController(
        IRepoWrapper repo,
        IEmailService emailService,
        ILogger<CheckoutController> logger,
        ShopContext db)
    {
        _repo = repo;
        _emailService = emailService;
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// Places or replays a customer/guest order. Resolves every line
    /// price server-side, applies a valid discount code (silently skips an
    /// invalid one), computes the totals, and persists the order with a unique
    /// order number.
    /// </summary>
    /// <param name="request">The guest checkout payload.</param>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderConfirmationResponse>>> PlaceOrder(
        [FromBody] PlaceOrderRequest request)
    {
        if (request.PaymentMethod != PaymentMethod.Cash)
            return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail("Card payments are unavailable until the store connects a payment provider. Please use cash on delivery.", 400));
        if (!request.IdempotencyKey.HasValue || request.IdempotencyKey == Guid.Empty)
            return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail("A unique checkout attempt key is required.", 400));
        if (Request.Headers.ContainsKey("Authorization") && User.Identity?.IsAuthenticated != true)
            return Unauthorized(ApiResponse<OrderConfirmationResponse>.Fail("Your session expired. Sign in again before placing the order.", 401));
        if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Customer"))
            return StatusCode(403, ApiResponse<OrderConfirmationResponse>.Fail("Use a customer account or guest checkout.", 403));
        Customer? customer = null;
        if (User.IsInRole("Customer"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) || (customer = await _db.Customers.FindAsync(id)) is null)
                return Unauthorized(ApiResponse<OrderConfirmationResponse>.Fail("Customer account is unavailable.", 401));
        }
        string key = request.IdempotencyKey.Value.ToString("N");
        string owner = customer is null ? "guest" : "customer:" + customer.Id;
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        await _db.Database.OpenConnectionAsync();
        try
        {
            var replay = await ReadReceipt(key, owner, hash);
            if (replay is not null) return replay;
            await using var transaction = await _db.Database.BeginTransactionAsync();
            replay = await ReadReceipt(key, owner, hash);
            if (replay is not null) return replay;
            try
            {
                var result = await PlaceCore(request, customer);
                if (result.Result is not OkObjectResult ok)
                {
                    Response.Headers["X-Checkout-Outcome"] = "NotCreated";
                    return result;
                }
                string json = JsonSerializer.Serialize(ok.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"CheckoutReceipts\" (\"Key\",\"Owner\",\"RequestHash\",\"ResponseJson\") VALUES ({0},{1},{2},{3})", key, owner, hash, json);
                // The order, receipt and queued notifications commit together.
                var confirmation = ((ApiResponse<OrderConfirmationResponse>)ok.Value!).Data!;
                    var order = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.OrderNumber == confirmation.OrderNumber);
                    var settings = await _db.StoreSettings.FirstAsync();
                    await _emailService.SendOrderSummaryAsync(order, settings);
                await transaction.CommitAsync();
                return result;
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
            {
                await transaction.RollbackAsync(); _db.ChangeTracker.Clear();
                return await ReadReceipt(key, owner, hash) ?? Conflict(ApiResponse<OrderConfirmationResponse>.Fail("Checkout is being processed. Retry the same submission.", 409));
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
            {
                await transaction.RollbackAsync(); _db.ChangeTracker.Clear();
                return await ReadReceipt(key, owner, hash) ?? Conflict(ApiResponse<OrderConfirmationResponse>.Fail("Checkout is being processed. Retry the same submission.", 409));
            }
        }
        finally { await _db.Database.CloseConnectionAsync(); }
    }

    private async Task<ActionResult<ApiResponse<OrderConfirmationResponse>>?> ReadReceipt(string key, string owner, string hash)
    {
        using var command = _db.Database.GetDbConnection().CreateCommand();
        command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT \"Owner\",\"RequestHash\",\"ResponseJson\" FROM \"CheckoutReceipts\" WHERE \"Key\"=@key";
        var parameter = command.CreateParameter(); parameter.ParameterName = "key"; parameter.Value = key; command.Parameters.Add(parameter);
        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        if (reader.GetString(0) != owner || reader.GetString(1) != hash)
            return Conflict(ApiResponse<OrderConfirmationResponse>.Fail("This checkout attempt was already used with different details. Start a new checkout attempt.", 409));
        var saved = JsonSerializer.Deserialize<ApiResponse<OrderConfirmationResponse>>(reader.GetString(2), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        if (saved.Data is not null)
            saved.Data.TrackingToken = HttpContext.RequestServices.GetRequiredService<Shop.Api.Tenancy.OrderTrackingAccess>().Create(saved.Data.OrderNumber);
        return Ok(saved);
    }

    private async Task<ActionResult<ApiResponse<OrderConfirmationResponse>>> PlaceCore(PlaceOrderRequest request, Customer? customer)
    {
        if (request.Lines is null || request.Lines.Count == 0)
        {
            return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail("Cart is empty/invalid", 400));
        }

        // Resolve each line against the live product/variant, building snapshot
        // order items with server-trusted prices.
        var items = new List<OrderItem>();
        foreach (CartLineRequest line in request.Lines)
        {
            Product? product = await _repo._Product.Query()
                .Include(p => p.Variants).ThenInclude(v => v.Size)
                .Include(p => p.Variants).ThenInclude(v => v.Color)
                .FirstOrDefaultAsync(p => p.Id == line.ProductId);
            if (product is null)
            {
                return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail(
                    $"Product {line.ProductId} not found", 400));
            }

            if (!product.IsActive || (product.HasVariants && !line.VariantId.HasValue))
                return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail("Select an available product and its size/color variant.", 400));

            long requestedQuantity = request.Lines
                .Where(l => l.ProductId == line.ProductId && l.VariantId == line.VariantId)
                .Sum(l => (long)l.Quantity);
            if (!line.VariantId.HasValue && requestedQuantity > product.StockQuantity)
                return Conflict(ApiResponse<OrderConfirmationResponse>.Fail("The requested product quantity exceeds available stock.", 409));

            decimal unitPrice = product.BasePrice;
            string nameSnapshot = product.Name;
            string? sizeSnapshot = null;
            string? colorSnapshot = null;

            if (line.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants
                    .FirstOrDefault(v => v.Id == line.VariantId.Value);
                if (variant is null)
                {
                    return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail(
                        $"Variant {line.VariantId.Value} not found for product {line.ProductId}", 400));
                }

                if (!variant.IsActive || requestedQuantity > variant.StockQuantity)
                    return Conflict(ApiResponse<OrderConfirmationResponse>.Fail("The selected size/color has insufficient stock or is unavailable.", 409));

                unitPrice = variant.Price;
                sizeSnapshot = variant.Size?.Name;
                colorSnapshot = variant.Color?.Name;

                var parts = new List<string>();
                if (sizeSnapshot is not null)
                {
                    parts.Add(sizeSnapshot);
                }

                if (colorSnapshot is not null)
                {
                    parts.Add(colorSnapshot);
                }

                nameSnapshot = parts.Count == 0
                    ? product.Name
                    : $"{product.Name} ({string.Join(", ", parts)})";
            }

            items.Add(new OrderItem
            {
                ProductId = product.Id,
                VariantId = line.VariantId,
                ProductNameSnapshot = nameSnapshot,
                SizeSnapshot = sizeSnapshot,
                ColorSnapshot = colorSnapshot,
                UnitPrice = unitPrice,
                Quantity = line.Quantity,
                LineTotal = unitPrice * line.Quantity,
                IsDemoData = false
            });
        }

        decimal subtotal = items.Sum(i => i.LineTotal);

        // Apply the discount code when present and currently valid; otherwise
        // the order is placed WITHOUT a discount (never a 400) and the response
        // explains why.
        decimal discountTotal = 0m;
        string? message = null;
        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            (discountTotal, message) = await ResolveDiscountAsync(request.DiscountCode, subtotal);
        }

        decimal total = subtotal - discountTotal;
        if (total < 0m)
        {
            total = 0m;
        }

        // Load the store settings for the order currency (fall back to USD/$ if
        // the single settings row is somehow missing).
        StoreSettings settings = await _repo._StoreSettings.Query().FirstOrDefaultAsync()
            ?? new StoreSettings { Id = 1, CurrencyCode = "USD", CurrencySymbol = "$" };
        var area = await AvailableArea(request.AreaId, settings.CurrencyCode);
        if (area is null) return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail("Delivery is unavailable for this area or store currency. Please select another area.", 400));
        total += area.ShippingFee;
        if ((request.ExpectedTotal.HasValue && request.ExpectedTotal.Value != total) ||
            (request.ExpectedCurrencyCode is not null && request.ExpectedCurrencyCode != settings.CurrencyCode))
            return Conflict(ApiResponse<OrderConfirmationResponse>.Fail("The total changed. Review the updated shipping and order total before placing the order again.", 409));

        var now = DateTime.UtcNow;
        var initialStatus = await _db.OrderStatusDefinitions.FirstAsync(s => s.IsDefault && s.IsActive);
        var order = new Order
        {
            CustomerId = customer?.Id,
            GuestName = customer?.FullName ?? request.GuestName,
            GuestEmail = customer?.Email ?? request.GuestEmail,
            Status = OrderStatus.Pending,
            StatusDefinitionId = initialStatus.Id,
            StatusDefinition = initialStatus,
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            Tax = 0m,
            ShippingTotal = area.ShippingFee,
            DeliveryAreaId = area.Id,
            Total = total,
            CurrencyCode = settings.CurrencyCode,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.CashOnDelivery,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = area.Name,
            StateRegion = area.Governorate!.Name,
            PostalCode = request.PostalCode,
            Country = request.Country,
            ContactPhone = request.ContactPhone,
            PlacedAt = now,
            IsDemoData = false,
            Items = items
        };

        order.OrderNumber = OrderNumberGenerator.Generate(now);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var confirmation = new OrderConfirmationResponse
        {
            TrackingToken = HttpContext.RequestServices.GetRequiredService<Shop.Api.Tenancy.OrderTrackingAccess>().Create(order.OrderNumber),
            OrderNumber = order.OrderNumber,
            Subtotal = order.Subtotal,
            DiscountTotal = order.DiscountTotal,
            ShippingTotal = order.ShippingTotal,
            Total = order.Total,
            Status = initialStatus.Name,
            PlacedAt = order.PlacedAt,
            CurrencyCode = order.CurrencyCode,
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            AddressLine1 = order.AddressLine1,
            AddressLine2 = order.AddressLine2,
            City = order.City,
            StateRegion = order.StateRegion,
            PostalCode = order.PostalCode,
            Country = order.Country,
            ContactPhone = order.ContactPhone
        };

        return Ok(ApiResponse<OrderConfirmationResponse>.Ok(confirmation, message));
    }

    private Task<DeliveryArea?> AvailableArea(int areaId, string currency) => _db.DeliveryAreas.AsNoTracking()
        .Include(a => a.Governorate).FirstOrDefaultAsync(a => a.Id == areaId && a.IsActive && a.Governorate!.IsActive && a.CurrencyCode == currency);

    [HttpPost("quote")]
    public async Task<ActionResult<ApiResponse<CheckoutQuote>>> Quote(CheckoutQuoteRequest request)
    {
        var currency = (await _db.StoreSettings.FirstAsync()).CurrencyCode;
        var area = await AvailableArea(request.AreaId, currency);
        if (area is null) return BadRequest(ApiResponse<CheckoutQuote>.Fail("Delivery is unavailable for the selected area.", 400));
        decimal subtotal = 0;
        foreach (var line in request.Lines.GroupBy(l => new { l.ProductId, l.VariantId }))
        {
            var product = await _db.Products.AsNoTracking().Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == line.Key.ProductId && p.IsActive);
            if (product is null) return BadRequest(ApiResponse<CheckoutQuote>.Fail("Product is unavailable.", 400));
            long quantity = line.Sum(l => (long)l.Quantity);
            decimal price = product.BasePrice;
            if (line.Key.VariantId.HasValue)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == line.Key.VariantId && v.IsActive);
                if (variant is null || variant.StockQuantity < quantity) return Conflict(ApiResponse<CheckoutQuote>.Fail("Selected size/color has insufficient stock.", 409));
                price = variant.Price;
            }
            else if (product.HasVariants || product.StockQuantity < quantity)
                return Conflict(ApiResponse<CheckoutQuote>.Fail("Select an available product size/color and quantity.", 409));
            subtotal += price * quantity;
        }
        var discount = string.IsNullOrWhiteSpace(request.DiscountCode) ? (0m, (string?)null) : await ResolveDiscountAsync(request.DiscountCode, subtotal);
        return Ok(ApiResponse<CheckoutQuote>.Ok(new CheckoutQuote { Subtotal = subtotal, DiscountTotal = discount.Item1,
            ShippingTotal = area.ShippingFee, Total = Math.Max(0, subtotal - discount.Item1) + area.ShippingFee, CurrencyCode = currency }, discount.Item2));
    }

    /// <summary>
    /// Resolves a discount code against the catalog and the current time.
    /// Returns the computed discount amount (0 when not applied) and, when the
    /// code is not applied, a message explaining the reason.
    /// </summary>
    /// <param name="code">The discount code supplied at checkout.</param>
    /// <param name="subtotal">The order subtotal the discount applies to.</param>
    private async Task<(decimal DiscountTotal, string? Message)> ResolveDiscountAsync(string code, decimal subtotal)
    {
        if(!Shop.Api.Tenancy.PlanFeatures.Allows(HttpContext.RequestServices.GetRequiredService<Shop.Api.Tenancy.DeploymentProfile>(),
            HttpContext.RequestServices.GetRequiredService<Shop.Api.Tenancy.StoreScope>(),"Discounts"))return (0m,"Discount codes are unavailable at this store.");
        Discount? discount = await _repo._Discount.GetByCodeAsync(code);
        if (discount is null)
        {
            return (0m, "Discount code not applied: unknown code");
        }

        if (!discount.IsActive)
        {
            return (0m, "Discount code not applied: inactive code");
        }

        DateTime now = DateTime.UtcNow;
        if ((discount.StartsAt.HasValue && now < discount.StartsAt.Value) ||
            (discount.EndsAt.HasValue && now > discount.EndsAt.Value))
        {
            return (0m, "Discount code not applied: expired code");
        }

        decimal amount = discount.Type == DiscountType.Percentage
            ? subtotal * (discount.Value / 100m)
            : discount.Value;

        // Never discount below zero.
        if (amount > subtotal)
        {
            amount = subtotal;
        }

        return (amount, null);
    }
}
