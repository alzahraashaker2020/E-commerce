namespace Shop.Application.Orders;

public enum FulfillmentOutcomeKind { Success, NotFound, Invalid, Conflict }
public sealed record FulfillmentOutcome(FulfillmentOutcomeKind Kind, string? Message = null);
public sealed record ChangeOrderStatusCommand(int OrderId, int? DefinitionId, FulfillmentStatus? LegacyStatus);
public sealed record FulfillmentLine(int ProductId, int? VariantId, int Quantity);
public sealed record FulfillmentOrder(int Id, FulfillmentStatus Status, int? DefinitionId, bool StockDeducted, IReadOnlyList<FulfillmentLine> Lines);
public sealed record FulfillmentDefinition(int Id, FulfillmentStatus Behavior);
public sealed class FulfillmentConcurrencyException : Exception { }
