namespace Shop.Application.Returns;

public interface IReturnStore
{
    Task<IReturnTransaction> BeginAsync(CancellationToken ct);
    Task<List<ReturnEntry>> ReadAsync(int? orderId, CancellationToken ct);
    Task<ReturnOrder> OrderAsync(int id, bool reload, CancellationToken ct);
    Task<ReturnLine?> LineAsync(int orderId, int itemId, CancellationToken ct);
    Task InsertAsync(string id, int orderId, int itemId, int quantity, string reason, decimal max, DateTime now, CancellationToken ct);
    Task<bool> UpdateAsync(ReturnEntry entry, int version, string next, string notes, bool restock, string action, decimal refund, string reference, CancellationToken ct);
    Task<bool> RestockAsync(ReturnLine line, int quantity, CancellationToken ct);
    Task AuditAsync(string id, string action, string actor, string notes, DateTime now, CancellationToken ct);
    Task QueueAsync(string to, string subject, string body, CancellationToken ct);
}
public interface IReturnTransaction : IAsyncDisposable { Task CommitAsync(CancellationToken ct); }
