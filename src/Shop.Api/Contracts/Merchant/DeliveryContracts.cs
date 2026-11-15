using Shop.DAL.Models;
namespace Shop.Api.Contracts.Merchant;

public sealed class GovernorateResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public static GovernorateResponse From(Governorate value) => new() { Id = value.Id, Name = value.Name, IsActive = value.IsActive };
}
public sealed class DeliveryAreaResponse
{
    public int Id { get; set; }
    public int GovernorateId { get; set; }
    public GovernorateResponse? Governorate { get; set; }
    public string Name { get; set; } = "";
    public decimal ShippingFee { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public static DeliveryAreaResponse From(DeliveryArea value) => new() { Id = value.Id, GovernorateId = value.GovernorateId, Governorate = value.Governorate is null ? null : GovernorateResponse.From(value.Governorate), Name = value.Name, ShippingFee = value.ShippingFee, CurrencyCode = value.CurrencyCode, IsActive = value.IsActive };
}
