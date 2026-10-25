namespace Shop.DAL.Models;

/// <summary>
/// An admin-managed color attribute (e.g. "Beige", "Grey"). Generic across any
/// product domain; it carries no domain-specific meaning in code. Referenced by
/// <see cref="ProductVariant"/> rows that opt into a color dimension.
/// </summary>
public class Color
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name of the color (unique).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional hex color code, e.g. "#808080" (max 9 chars).</summary>
    public string? HexCode { get; set; }

    /// <summary>Sort position used to order colors in pickers and lists.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the color is active and selectable.</summary>
    public bool IsActive { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Variants that reference this color.</summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
