using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Public guest checkout endpoint. Totals are computed server-side from trusted
/// prices; the order number is generated and retried on the rare unique-index
/// collision. An unknown/inactive/expired discount code never fails the order —
/// it is placed without the discount and the response explains why.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private const int OrderNumberMaxAttempts = 3;

    private readonly IRepoWrapper _repo;

    /// <summary>
    /// Creates a new <see cref="CheckoutController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    public CheckoutController(IRepoWrapper repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Places a guest order from the supplied request. Resolves every line
    /// price server-side, applies a valid discount code (silently skips an
    /// invalid one), computes the totals, and persists the order with a unique
    /// order number.
    /// </summary>
    /// <param name="request">The guest checkout payload.</param>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderConfirmationResponse>>> PlaceOrder(
        [FromBody] PlaceOrderRequest request)
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
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == line.ProductId);
            if (product is null)
            {
                return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail(
                    $"Product {line.ProductId} not found", 400));
            }

            decimal unitPrice = product.BasePrice;
            string nameSnapshot = product.Name;

            if (line.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants
                    .FirstOrDefault(v => v.Id == line.VariantId.Value);
                if (variant is null)
                {
                    return BadRequest(ApiResponse<OrderConfirmationResponse>.Fail(
                        $"Variant {line.VariantId.Value} not found for product {line.ProductId}", 400));
                }

                unitPrice = variant.Price;
                nameSnapshot = $"{product.Name} ({variant.OptionName}: {variant.OptionValue})";
            }

            items.Add(new OrderItem
            {
                ProductId = product.Id,
                VariantId = line.VariantId,
                ProductNameSnapshot = nameSnapshot,
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

        var now = DateTime.UtcNow;
        var order = new Order
        {
            CustomerId = null,
            GuestName = request.GuestName,
            GuestEmail = request.GuestEmail,
            Status = OrderStatus.Pending,
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            Tax = 0m,
            ShippingTotal = 0m,
            Total = total,
            PlacedAt = now,
            IsDemoData = false,
            Items = items
        };

        // Generate a unique order number, retrying on the rare unique-index
        // collision (the index is the real guarantee; this is the safety net).
        for (int attempt = 1; attempt <= OrderNumberMaxAttempts; attempt++)
        {
            order.OrderNumber = OrderNumberGenerator.Generate(now);
            try
            {
                _repo._Order.Add(order);
                _repo.Save();
                break;
            }
            catch (DbUpdateException) when (attempt < OrderNumberMaxAttempts)
            {
                // Likely an OrderNumber unique-violation: detach the attempt and
                // regenerate on the next pass.
                _repo._Order.Delete(order);
                order.Id = 0;
            }
            catch (DbUpdateException)
            {
                // Exhausted retries or a different persistence failure.
                return StatusCode(500,
                    ApiResponse<OrderConfirmationResponse>.Fail("Could not place order", 500));
            }
        }

        var confirmation = new OrderConfirmationResponse
        {
            OrderNumber = order.OrderNumber,
            Total = order.Total,
            Status = order.Status.ToString(),
            PlacedAt = order.PlacedAt
        };

        return Ok(ApiResponse<OrderConfirmationResponse>.Ok(confirmation, message));
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
