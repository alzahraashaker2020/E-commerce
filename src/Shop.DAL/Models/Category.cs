namespace Shop.DAL.Models;

/// <summary>
/// A product category used to group and filter the catalog.
///
/// Categories form a two-level tree via the self-reference
/// <see cref="ParentCategoryId"/>: top-level categories (e.g. "Sofa Styling")
/// have a null parent, while subcategories (e.g. "Sofa covers") point at their
/// parent. Products are attached to the leaf (subcategory) level.
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

    /// <summary>
    /// Optional image URL shown on the category card (relative to the Angular
    /// assets folder for seeded demo categories).
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Parent category id, or null for a top-level category. Enables the
    /// category → subcategory hierarchy.
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>Navigation to the parent category (null when top-level).</summary>
    public Category? ParentCategory { get; set; }

    /// <summary>Child subcategories of this category.</summary>
    public ICollection<Category> Children { get; set; } = new List<Category>();

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Products belonging to this category.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
