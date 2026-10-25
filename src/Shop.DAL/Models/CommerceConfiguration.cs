namespace Shop.DAL.Models;

/// <summary>Admin-managed store currency, without exchange-rate conversion.</summary>
public class Currency
{
    /// <summary>Configured Id.</summary>
    public int Id { get; set; }
    /// <summary>Configured Code.</summary>
    public string Code { get; set; } = "";
    /// <summary>Configured Name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Configured Symbol.</summary>
    public string Symbol { get; set; } = "";
    /// <summary>Configured IsActive.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Admin-managed display status mapped to a safe fulfillment behavior.</summary>
public class OrderStatusDefinition
{
    /// <summary>Configured Id.</summary>
    public int Id { get; set; }
    /// <summary>Configured Name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Configured Color.</summary>
    public string Color { get; set; } = "#8a776e";
    /// <summary>Configured Behavior.</summary>
    public OrderStatus Behavior { get; set; }
    /// <summary>Configured DisplayOrder.</summary>
    public int DisplayOrder { get; set; }
    /// <summary>Configured IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Configured IsDefault.</summary>
    public bool IsDefault { get; set; }
}
