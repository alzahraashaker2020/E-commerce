using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;

/// <summary>
/// Public cart endpoint. Stateless: it re-prices each requested line against
/// the live product/variant server-side and never reads or writes a
/// <c>Cart</c>/<c>CartItem</c> row. Any client-supplied price is ignored.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly IRepoWrapper _repo;

    /// <summary>
    /// Creates a new <see cref="CartController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    public CartController(IRepoWrapper repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Validates and prices a set of cart lines. Each line's product (and
    /// variant, when supplied) is resolved server-side; unknown products or
    /// variants produce a 400 failure with a per-line message.
    /// </summary>
    /// <param name="lines">The raw requested cart lines.</param>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<CartVM>>> Validate([FromBody] List<CartLineRequest> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            return BadRequest(ApiResponse<CartVM>.Fail("Cart is empty/invalid", 400));
        }

        var cart = new CartVM();

        foreach (CartLineRequest line in lines)
        {
            // Load the product with its variants so variant pricing resolves.
            Product? product = await _repo._Product.Query()
                .Include(p => p.Variants).ThenInclude(v => v.Size)
                .Include(p => p.Variants).ThenInclude(v => v.Color)
                .FirstOrDefaultAsync(p => p.Id == line.ProductId);
            if (product is null)
            {
                return BadRequest(ApiResponse<CartVM>.Fail(
                    $"Product {line.ProductId} not found", 400));
            }

            // Resolve the unit price server-side: variant price when a variant
            // is specified, otherwise the product base price. Never trust a
            // client-supplied price.
            decimal unitPrice = product.BasePrice;
            string productName = product.Name;

            if (line.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants
                    .FirstOrDefault(v => v.Id == line.VariantId.Value);
                if (variant is null)
                {
                    return BadRequest(ApiResponse<CartVM>.Fail(
                        $"Variant {line.VariantId.Value} not found for product {line.ProductId}", 400));
                }

                unitPrice = variant.Price;
                productName = ComposeVariantName(product.Name, variant);
            }

            var cartLine = new CartLineVM
            {
                ProductId = product.Id,
                VariantId = line.VariantId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = line.Quantity,
                LineTotal = unitPrice * line.Quantity
            };

            cart.Lines.Add(cartLine);
        }

        cart.Subtotal = cart.Lines.Sum(l => l.LineTotal);

        return Ok(ApiResponse<CartVM>.Ok(cart));
    }

    /// <summary>
    /// Composes a display name for a variant line from the product name and the
    /// variant's size/color (whichever are present). Falls back to the product
    /// name when the variant carries neither dimension.
    /// </summary>
    /// <param name="productName">The owning product name.</param>
    /// <param name="variant">The resolved variant.</param>
    private static string ComposeVariantName(string productName, ProductVariant variant)
    {
        var parts = new List<string>();
        if (variant.Size is not null)
        {
            parts.Add(variant.Size.Name);
        }

        if (variant.Color is not null)
        {
            parts.Add(variant.Color.Name);
        }

        return parts.Count == 0
            ? productName
            : $"{productName} ({string.Join(", ", parts)})";
    }
}
