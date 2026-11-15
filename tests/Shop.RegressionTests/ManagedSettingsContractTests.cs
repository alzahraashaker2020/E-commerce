using System.Text.Json;
using System.Text.Json.Serialization;
using Shop.Api.Contracts.Merchant;
using Shop.DAL.Models;
using Xunit;
namespace Shop.RegressionTests;

public class ManagedSettingsContractTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    [Fact] public void Currency_keeps_all_existing_fields() => Assert.Equal(JsonSerializer.Serialize(new Currency { Id = 2, Code = "EGP", Name = "Pound", Symbol = "E", IsActive = false }, Options), JsonSerializer.Serialize(CurrencyResponse.From(new() { Id = 2, Code = "EGP", Name = "Pound", Symbol = "E", IsActive = false }), Options));
    [Fact]
    public void Status_keeps_behavior_enum_and_default_flags()
    {
        var entity = new OrderStatusDefinition { Id = 5, Name = "Complete", Color = "#123456", Behavior = OrderStatus.Delivered, DisplayOrder = 4, IsActive = false, IsDefault = true };
        Assert.Equal(JsonSerializer.Serialize(entity, Options), JsonSerializer.Serialize(OrderStatusDefinitionResponse.From(entity), Options));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Delivery_keeps_nested_governorate_or_explicit_null(bool loaded)
    {
        var area = new DeliveryArea { Id = 3, GovernorateId = 4, Governorate = loaded ? new() { Id = 4, Name = "Region", IsActive = false } : null, Name = "Area", CurrencyCode = "USD", ShippingFee = 12.25m, IsActive = false };
        Assert.Equal(JsonSerializer.Serialize(area, Options), JsonSerializer.Serialize(DeliveryAreaResponse.From(area), Options));
    }
}
