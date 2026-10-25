namespace Shop.DAL.Models;

/// <summary>Admin-managed delivery governorate.</summary>
public class Governorate
{
    /// <summary>Identifier.</summary>
    public int Id { get; set; }
    /// <summary>Governorate name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Whether delivery is available.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Admin-managed delivery area and its current shipping rate.</summary>
public class DeliveryArea
{
    /// <summary>Identifier.</summary>
    public int Id { get; set; }
    /// <summary>Governorate identifier.</summary>
    public int GovernorateId { get; set; }
    /// <summary>Governorate.</summary>
    public Governorate? Governorate { get; set; }
    /// <summary>Area name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Shipping fee; zero explicitly means free delivery.</summary>
    public decimal ShippingFee { get; set; }
    /// <summary>Currency of the configured fee.</summary>
    public string CurrencyCode { get; set; } = "";
    /// <summary>Whether delivery is available.</summary>
    public bool IsActive { get; set; } = true;
}
