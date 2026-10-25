namespace Shop.DAL.Models;

/// <summary>
/// An admin-managed size attribute (e.g. "Small", "Large"). Generic across any
/// product domain; it carries no domain-specific meaning in code. Referenced by
/// <see cref="ProductVariant"/> rows that opt into a size dimension.
/// </summary>
public class Size
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name of the size (unique).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Sort position used to order sizes in pickers and lists.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the size is active and selectable.</summary>
    public bool IsActive { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Variants that reference this size.</summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
