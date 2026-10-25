using System.ComponentModel.DataAnnotations;
using Shop.DAL.Models;

namespace Shop.DAL.ViewModels;

/// <summary>Editable currency metadata.</summary>
public class CurrencyRequest
{
    /// <summary>Configured Code.</summary>
    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string Code { get; set; } = "";
    /// <summary>Configured Name.</summary>
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    /// <summary>Configured Symbol.</summary>
    [Required, StringLength(8)]
    public string Symbol { get; set; } = "";
    /// <summary>Configured IsActive.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Editable order status metadata and fulfillment behavior.</summary>
public class StatusDefinitionRequest
{
    /// <summary>Configured Name.</summary>
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    /// <summary>Configured Color.</summary>
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string Color { get; set; } = "#8a776e";
    /// <summary>Configured Behavior.</summary>
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Behavior { get; set; }
    /// <summary>Configured DisplayOrder.</summary>
    [Range(0, 10000)]
    public int DisplayOrder { get; set; }
    /// <summary>Configured IsActive.</summary>
    public bool IsActive { get; set; } = true;
}
