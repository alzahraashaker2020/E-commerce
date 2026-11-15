namespace Shop.Application.Returns;

public sealed record ReturnEntry(string Id, int OrderId, int OrderItemId, int Quantity, string Reason, string State, bool Restocked, decimal MaxRefund, decimal RefundAmount, string RefundReference, string Notes, int Version, string CreatedAtUtc, string OrderNumber, string CurrencyCode, string ProductName);
public sealed record ReturnOrder(int Id, bool Delivered, bool StockDeducted, decimal Total, decimal ShippingTotal, decimal Subtotal, string OrderNumber, string CurrencyCode, string? CustomerEmail, string? GuestEmail);
public sealed record ReturnLine(int Id, int ProductId, int? VariantId, int Quantity, decimal LineTotal);
