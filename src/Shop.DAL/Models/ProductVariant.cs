namespace Shop.DAL.Models;

/// <summary>
/// A purchasable variant of a <see cref="Product"/>, e.g. a specific size or
/// color combination.
/// </summary>
public class ProductVariant
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="Product"/>.</summary>
    public int ProductId { get; set; }

    /// <summary>Stock-keeping unit identifier.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Name of the option dimension, e.g. "Size".</summary>
    public string OptionName { get; set; } = string.Empty;

    /// <summary>Value of the option, e.g. "Large".</summary>
    public string OptionValue { get; set; } = string.Empty;

    /// <summary>Selling price of this variant.</summary>
    public decimal Price { get; set; }

    /// <summary>Optional strike-through / compare-at price.</summary>
    public decimal? CompareAtPrice { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning product navigation.</summary>
    public Product? Product { get; set; }
}
