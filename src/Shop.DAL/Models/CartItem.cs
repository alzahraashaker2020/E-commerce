namespace Shop.DAL.Models;

/// <summary>
/// A single line within a <see cref="Cart"/>. Schema-only in this scaffold.
/// <see cref="UnitPriceSnapshot"/> is display-only and never trusted for money
/// computation; prices are always re-resolved server-side.
/// </summary>
public class CartItem
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="Cart"/>.</summary>
    public int CartId { get; set; }

    /// <summary>Foreign key to the referenced <see cref="Product"/>.</summary>
    public int ProductId { get; set; }

    /// <summary>Optional foreign key to a specific <see cref="ProductVariant"/>.</summary>
    public int? VariantId { get; set; }

    /// <summary>Quantity of this line.</summary>
    public int Quantity { get; set; }

    /// <summary>Last-seen unit price, for display only.</summary>
    public decimal UnitPriceSnapshot { get; set; }

    /// <summary>Owning cart navigation.</summary>
    public Cart? Cart { get; set; }

    /// <summary>Referenced product navigation.</summary>
    public Product? Product { get; set; }

    /// <summary>Referenced variant navigation, when set.</summary>
    public ProductVariant? Variant { get; set; }
}
