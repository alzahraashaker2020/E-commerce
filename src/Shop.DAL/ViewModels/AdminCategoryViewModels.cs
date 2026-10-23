using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Create/update payload for an admin category. The slug is derived server-side
/// from the name, so it is not accepted from the client. A null
/// <see cref="ParentCategoryId"/> creates a top-level category; a non-null value
/// creates a subcategory of that parent.
/// </summary>
public class CategoryUpsertRequest
{
    /// <summary>Display name of the category (required, max 120 chars).</summary>
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional image URL shown on the category card.</summary>
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Parent category id, or null for a top-level category.
    /// </summary>
    public int? ParentCategoryId { get; set; }
}

/// <summary>
/// Admin list row for a category. Carries the parent name (null for top-level)
/// plus the child and product counts used to drive the delete guard in the UI.
/// </summary>
public class AdminCategoryListItemVM
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name of the category.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug (server-derived).</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional image URL shown on the category card.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Parent category id, or null for a top-level category.</summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>Parent category name, or null for a top-level category.</summary>
    public string? ParentName { get; set; }

    /// <summary>Number of products attached to this category.</summary>
    public int ProductCount { get; set; }

    /// <summary>Number of subcategories under this category.</summary>
    public int ChildCount { get; set; }
}
