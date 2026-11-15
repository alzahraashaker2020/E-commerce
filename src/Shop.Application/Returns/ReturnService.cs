using System.Globalization;
using Shop.Application.Common;
namespace Shop.Application.Returns;

public sealed class ReturnService
{
    private readonly IReturnStore _store; private readonly IClock _clock;
    public ReturnService(IReturnStore store, IClock clock) { _store = store; _clock = clock; }
    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
    public Task<List<ReturnEntry>> ReadAsync(int? orderId = null, CancellationToken ct = default) => _store.ReadAsync(orderId, ct);
    public async Task<ReturnEntry> RequestAsync(int orderId, Guid key, int itemId, int quantity, string reason, CancellationToken ct = default)
    {
        await using var transaction = await _store.BeginAsync(ct);
        var entries = await _store.ReadAsync(orderId, ct); string id = key.ToString("N");
        var previous = entries.FirstOrDefault(e => e.Id == id);
        if (previous is not null) { if (previous.OrderItemId != itemId || previous.Quantity != quantity || previous.Reason != reason) throw new InvalidOperationException("This return attempt was already used with different details."); return previous; }
        // Reload status inside the transaction, rather than trusting the access-check snapshot.
        var order = await _store.OrderAsync(orderId, true, ct);
        if (!order.Delivered || !order.StockDeducted) throw new InvalidOperationException("Only delivered orders can be returned.");
        var line = await _store.LineAsync(order.Id, itemId, ct);
        if (line is null || quantity < 1 || quantity > line.Quantity) throw new InvalidOperationException("Select a valid order item and quantity.");
        long reserved = entries.Where(e => e.OrderItemId == itemId && e.State != "Rejected").Sum(e => (long)e.Quantity);
        if (reserved + quantity > line.Quantity) throw new InvalidOperationException("This quantity already has a return request.");
        decimal max = ReturnPolicy.MaximumRefund(order.Total, order.ShippingTotal, order.Subtotal, line.LineTotal, quantity, line.Quantity);
        await _store.InsertAsync(id, order.Id, itemId, quantity, reason, max, _clock.UtcNow, ct);
        await _store.AuditAsync(id, "Requested", "Customer", reason, _clock.UtcNow, ct);
        await transaction.CommitAsync(ct); return (await _store.ReadAsync(order.Id, ct)).Single(e => e.Id == id);
    }
    public async Task<ReturnEntry> ChangeAsync(string id, int version, string action, string notes, bool restock, decimal refund, string reference, string actor, CancellationToken ct = default)
    {
        await using var transaction = await _store.BeginAsync(ct);
        var entry = (await _store.ReadAsync(null, ct)).SingleOrDefault(e => e.Id == id) ?? throw new InvalidOperationException("Return not found.");
        if (entry.Version != version) throw new InvalidOperationException("This return changed. Refresh and try again.");
        string next = ReturnPolicy.Next(entry.State, action, notes, restock, refund, entry.MaxRefund, reference);
        if (!await _store.UpdateAsync(entry, version, next, notes, restock, action, refund, reference, ct)) throw new InvalidOperationException("This return changed. Refresh and try again.");
        var order = await _store.OrderAsync(entry.OrderId, false, ct);
        if (action == "Receive" && restock)
        {
            if (!order.StockDeducted) throw new InvalidOperationException("This order did not consume stock.");
            var item = await _store.LineAsync(entry.OrderId, entry.OrderItemId, ct) ?? throw new InvalidOperationException("Sequence contains no elements.");
            if (!await _store.RestockAsync(item, entry.Quantity, ct)) throw new InvalidOperationException("The original stock item is unavailable. Inspect without restocking or restore the original item first.");
        }
        await _store.AuditAsync(id, action, actor, notes, _clock.UtcNow, ct);
        string to = order.CustomerEmail ?? order.GuestEmail ?? "";
        await _store.QueueAsync(to, $"Return update for order {order.OrderNumber}", $"Return {id}\nOrder {order.OrderNumber}\nProduct: {entry.ProductName}\nQuantity: {entry.Quantity}\nStatus: {next}\nNotes: {notes}\nRecorded refund: {(action == "RecordRefund" ? Money(refund) : Money(entry.RefundAmount))} {order.CurrencyCode}\nRefund records reflect a manual payment; this system has not transferred funds.", ct);
        await transaction.CommitAsync(ct); return (await _store.ReadAsync(entry.OrderId, ct)).Single(e => e.Id == id);
    }

}
