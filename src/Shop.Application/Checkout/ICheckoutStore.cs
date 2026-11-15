namespace Shop.Application.Checkout;

public interface ICheckoutStore
{
    Task<CheckoutCustomer?> CustomerAsync(int id, CancellationToken ct);
    Task<ICheckoutSession> OpenAsync(CancellationToken ct);
    Task<CheckoutProduct?> ProductAsync(int id, bool activeOnly, CancellationToken ct);
    Task<CheckoutSettings?> SettingsAsync(bool required, CancellationToken ct);
    Task<CheckoutArea?> AreaAsync(int id, string currency, CancellationToken ct);
    Task<DiscountSnapshot?> DiscountAsync(string code, CancellationToken ct);
    Task<InitialStatus> InitialStatusAsync(CancellationToken ct);
    Task SaveOrderAsync(CheckoutOrder order, CancellationToken ct);
    Task QueueSummaryAsync(string orderNumber, CancellationToken ct);
}
public interface ICheckoutSession : IAsyncDisposable
{
    Task<ReceiptSnapshot?> ReceiptAsync(string key, CancellationToken ct);
    Task BeginAsync(CancellationToken ct);
    Task WriteReceiptAsync(string key, string owner, string hash, string json, CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
public interface ICheckoutBoundary
{
    bool DiscountsAllowed { get; }
    string CreateTrackingProof(string orderNumber);
    string NewOrderNumber(DateTime now);
    string EncodeReceipt(CheckoutConfirmation confirmation, string? message);
}
