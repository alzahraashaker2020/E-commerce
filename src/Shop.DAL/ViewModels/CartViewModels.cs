using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// A single requested cart/checkout line. Prices are always resolved
/// server-side; any client-supplied price is ignored.
/// </summary>
public class CartLineRequest
{
    /// <summary>Referenced product identifier.</summary>
    [Required]
    public int ProductId { get; set; }

    /// <summary>Optional variant identifier.</summary>
    public int? VariantId { get; set; }

    /// <summary>Requested quantity (1-100).</summary>
    [Range(1, 100)]
    public int Quantity { get; set; }
}

/// <summary>
/// A server-priced cart.
/// </summary>
public class CartVM
{
    /// <summary>Priced cart lines.</summary>
    public List<CartLineVM> Lines { get; set; } = new List<CartLineVM>();

    /// <summary>Sum of all line totals.</summary>
    public decimal Subtotal { get; set; }
}

/// <summary>
/// A server-priced cart line.
/// </summary>
public class CartLineVM
{
    /// <summary>Referenced product identifier.</summary>
    public int ProductId { get; set; }

    /// <summary>Referenced variant identifier, when set.</summary>
    public int? VariantId { get; set; }

    /// <summary>Resolved product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Server-resolved unit price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Line quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Line total (<see cref="UnitPrice"/> * <see cref="Quantity"/>).</summary>
    public decimal LineTotal { get; set; }
}
