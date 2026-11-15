using Shop.DAL.Models;
namespace Shop.Api.Contracts.Merchant;

public sealed class CurrencyResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public static CurrencyResponse From(Currency value) => new() { Id = value.Id, Code = value.Code, Name = value.Name, Symbol = value.Symbol, IsActive = value.IsActive };
}
