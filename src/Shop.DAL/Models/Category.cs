namespace Shop.DAL.Models;

/// <summary>
/// A product category used to group and filter the catalog.
/// </summary>
public class Category
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name of the category.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly unique slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Products belonging to this category.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
