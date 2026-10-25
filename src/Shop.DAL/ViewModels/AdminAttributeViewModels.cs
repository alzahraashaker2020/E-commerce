using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Read representation of an admin-managed <c>Size</c>.
/// </summary>
public class SizeVM
{
    /// <summary>Size identifier.</summary>
    public int Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Sort position.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the size is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Create/update payload for a <c>Size</c>.
/// </summary>
public class SizeUpsertRequest
{
    /// <summary>Display name (required, max 100 chars).</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Sort position.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the size is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Read representation of an admin-managed <c>Color</c>.
/// </summary>
public class ColorVM
{
    /// <summary>Color identifier.</summary>
    public int Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional hex color code.</summary>
    public string? HexCode { get; set; }

    /// <summary>Sort position.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the color is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Create/update payload for a <c>Color</c>.
/// </summary>
public class ColorUpsertRequest
{
    /// <summary>Display name (required, max 100 chars).</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional hex color code (max 9 chars).</summary>
    [StringLength(9)]
    public string? HexCode { get; set; }

    /// <summary>Sort position.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the color is active.</summary>
    public bool IsActive { get; set; }
}
