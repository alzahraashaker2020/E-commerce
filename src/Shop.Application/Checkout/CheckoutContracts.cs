namespace Shop.Application.Checkout;

public class CheckoutInput
{
    public Guid? IdempotencyKey { get; set; }
    public int AreaId { get; set; }
    public decimal? ExpectedTotal { get; set; }
    public string? ExpectedCurrencyCode { get; set; }
    public string GuestName { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public List<CheckoutLine> Lines { get; set; } = new List<CheckoutLine>();

    public string? DiscountCode { get; set; }

    public string AddressLine1 { get; set; } = string.Empty;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string StateRegion { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public int PaymentMethod { get; set; }
}
public class CheckoutConfirmation
{
    public string TrackingToken { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime PlacedAt { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string StateRegion { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;
}
public class CheckoutLine
{
    public int ProductId { get; set; }

    public int? VariantId { get; set; }

    public int Quantity { get; set; }
}
public class QuoteSnapshot
{
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "";
}
public class QuoteInput
{
    public int AreaId { get; set; }
    public List<CheckoutLine> Lines { get; set; } = new();
    public string? DiscountCode { get; set; }
}
public enum CheckoutOutcomeKind { Success, Invalid, Unauthenticated, Forbidden, Conflict }
public sealed record CheckoutOutcome(CheckoutOutcomeKind Kind, CheckoutConfirmation? Confirmation = null, string? Message = null, string? ReplayJson = null, bool NotCreated = false);
public sealed record QuoteOutcome(CheckoutOutcomeKind Kind, QuoteSnapshot? Quote = null, string? Message = null);
public sealed record CheckoutActor(bool HasAuthorizationHeader, bool Authenticated, bool IsCustomer, int? CustomerId);
public sealed record CheckoutCustomer(int Id, string FullName, string Email);
public sealed record ReceiptSnapshot(string Owner, string Hash, string Json);
public sealed record CheckoutLabel(string Name);
public sealed record CheckoutVariant(int Id, bool IsActive, int StockQuantity, decimal Price, CheckoutLabel? Size, CheckoutLabel? Color);
public sealed record CheckoutProduct(int Id, string Name, bool IsActive, bool HasVariants, int StockQuantity, decimal BasePrice, IReadOnlyList<CheckoutVariant> Variants);
public sealed record CheckoutSettings(string CurrencyCode, string CurrencySymbol);
public sealed record CheckoutArea(int Id, string Name, decimal ShippingFee, CheckoutLabel? Governorate);
public sealed record InitialStatus(int Id, string Name);
public sealed class PricedLine
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string ProductNameSnapshot { get; set; } = ""; public string? SizeSnapshot { get; set; }
    public string? ColorSnapshot { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsDemoData { get; set; }
}
public sealed class CheckoutOrder
{
    public int? CustomerId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string Status { get; set; } = "Pending"; public int? StatusDefinitionId { get; set; }
    public InitialStatus? StatusDefinition { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingTotal { get; set; }
    public int? DeliveryAreaId { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "";
    public string PaymentMethod { get; set; } = "Cash"; public string PaymentStatus { get; set; } = "CashOnDelivery"; public string AddressLine1 { get; set; } = ""; public string? AddressLine2 { get; set; }
    public string City { get; set; } = ""; public string StateRegion { get; set; } = ""; public string PostalCode { get; set; } = ""; public string Country { get; set; } = ""; public string ContactPhone { get; set; } = ""; public DateTime PlacedAt { get; set; }
    public bool IsDemoData { get; set; }
    public List<PricedLine> Items { get; set; } = new(); public string OrderNumber { get; set; } = "";
}
public sealed class CheckoutDuplicateException : Exception { }
