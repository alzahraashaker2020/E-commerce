using Shop.DAL.Models;
namespace Shop.Api.Contracts.Merchant;

public sealed class OrderStatusDefinitionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#8a776e";
    public OrderStatus Behavior { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public static OrderStatusDefinitionResponse From(OrderStatusDefinition value) => new() { Id = value.Id, Name = value.Name, Color = value.Color, Behavior = value.Behavior, DisplayOrder = value.DisplayOrder, IsActive = value.IsActive, IsDefault = value.IsDefault };
}
