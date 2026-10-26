using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Merchant;

/// <summary>
/// Merchant inventory endpoint. JWT-protected under <c>api/merchant/inventory</c>.
/// </summary>
[ApiController]
[Authorize(Roles = "Merchant")]
[Route("api/merchant/inventory")]
public class MerchantInventoryController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="MerchantInventoryController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public MerchantInventoryController(IRepoWrapper repo, IMapper mapper)
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
        // Variant-level low stock: a variant at/below its own threshold. Load
        // the products that have low variants with their variant graph, then
        // flatten/project in memory.
        List<Product> productsWithVariants = await _repo._Product.Query()
            .Where(p => p.Variants.Any(v => v.StockQuantity <= v.LowStockThreshold))
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .ToListAsync();

        List<LowStockItemVM> variantLow = productsWithVariants
            .SelectMany(p => p.Variants
                .Where(v => v.StockQuantity <= v.LowStockThreshold)
                .Select(v => new LowStockItemVM
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    VariantId = v.Id,
                    VariantLabel = ComposeVariantLabel(v.Size?.Name, v.Color?.Name),
                    StockQuantity = v.StockQuantity,
                    LowStockThreshold = v.LowStockThreshold
                }))
            .ToList();

        // Product-level low stock: a no-variant product at/below its threshold.
        List<LowStockItemVM> productLow = await _repo._Product.Query()
            .Where(p => !p.HasVariants && p.StockQuantity <= p.LowStockThreshold)
            .Select(p => new LowStockItemVM
            {
                ProductId = p.Id,
                ProductName = p.Name,
                VariantId = null,
                VariantLabel = null,
                StockQuantity = p.StockQuantity,
                LowStockThreshold = p.LowStockThreshold
            })
            .ToListAsync();

        List<LowStockItemVM> lowStock = productLow.Concat(variantLow).ToList();

        return Ok(ApiResponse<List<LowStockItemVM>>.Ok(lowStock));
    }

    /// <summary>
    /// Builds a size/color label for a variant, or null when neither is set.
    /// </summary>
    /// <param name="sizeName">Size name, if assigned.</param>
    /// <param name="colorName">Color name, if assigned.</param>
    private static string? ComposeVariantLabel(string? sizeName, string? colorName)
    {
        if (sizeName is null && colorName is null)
        {
            return null;
        }

        if (sizeName is not null && colorName is not null)
        {
            return sizeName + " / " + colorName;
        }

        return sizeName ?? colorName;
    }
}
