namespace Shop.DAL.Models;

/// <summary>
/// An image associated with a <see cref="Product"/>.
/// </summary>
public class ProductImage
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="Product"/>.</summary>
    public int ProductId { get; set; }

    /// <summary>Absolute or relative image URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Accessible alternate text.</summary>
    public string? AltText { get; set; }

    /// <summary>Ordering position within the product's gallery.</summary>
    public int SortOrder { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Owning product navigation.</summary>
    public Product? Product { get; set; }
}
