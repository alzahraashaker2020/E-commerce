namespace Shop.DAL.ViewModels;

/// <summary>
/// Sales overview / revenue summary for the admin dashboard.
/// </summary>
public class SalesOverviewVM
{
    /// <summary>Total revenue over the window.</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Number of orders over the window.</summary>
    public int OrderCount { get; set; }

    /// <summary>Average order value.</summary>
    public decimal AverageOrderValue { get; set; }

    /// <summary>Count of inventory rows currently at/below threshold.</summary>
    public int LowStockCount { get; set; }

    /// <summary>Top-selling products.</summary>
    public List<TopProductVM> TopProducts { get; set; } = new List<TopProductVM>();
}

/// <summary>
/// A top-selling product entry within the sales overview.
/// </summary>
public class TopProductVM
{
    /// <summary>Product identifier.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Units sold.</summary>
    public int QuantitySold { get; set; }

    /// <summary>Revenue attributed to the product.</summary>
    public decimal Revenue { get; set; }
}
