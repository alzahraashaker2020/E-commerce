using System.ComponentModel.DataAnnotations;
using Shop.DAL.Models;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Discount representation.
/// </summary>
public class DiscountVM
{
    /// <summary>Discount identifier.</summary>
    public int Id { get; set; }

    /// <summary>Discount code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Discount type (Percentage or Fixed).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Discount value.</summary>
    public decimal Value { get; set; }

    /// <summary>Whether the discount is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional UTC start of validity.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Optional UTC end of validity.</summary>
    public DateTime? EndsAt { get; set; }
}

/// <summary>
/// Create/update payload for a discount.
/// </summary>
public class DiscountUpsertRequest
{
    /// <summary>Discount code (required, max 40 chars, unique).</summary>
    [Required]
    [StringLength(40)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Discount type (must be a defined value).</summary>
    [Required]
    [EnumDataType(typeof(DiscountType))]
    public DiscountType Type { get; set; }

    /// <summary>Discount value (must be &gt;= 0).</summary>
    [Range(0, double.MaxValue)]
    public decimal Value { get; set; }

    /// <summary>Whether the discount is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional UTC start of validity.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Optional UTC end of validity.</summary>
    public DateTime? EndsAt { get; set; }
}
