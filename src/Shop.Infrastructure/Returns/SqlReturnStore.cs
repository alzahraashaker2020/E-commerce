using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Application.Returns;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.BLL.Email;
namespace Shop.Infrastructure.Returns;

public sealed class SqlReturnStore : IReturnStore
{
    private readonly ShopContext _db; private readonly IEmailOutbox _mail;
    public SqlReturnStore(ShopContext db, IEmailOutbox mail) { _db = db; _mail = mail; }
    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    public async Task<IReturnTransaction> BeginAsync(CancellationToken ct) => new Transaction(await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct));
    public async Task<List<ReturnEntry>> ReadAsync(int? orderId, CancellationToken ct)
    {
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var command = _db.Database.GetDbConnection().CreateCommand(); command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT r.\"Id\",r.\"OrderId\",r.\"OrderItemId\",r.\"Quantity\",r.\"Reason\",r.\"State\",r.\"Restocked\",r.\"MaxRefund\",r.\"RefundAmount\",r.\"RefundReference\",r.\"Notes\",r.\"Version\",r.\"CreatedAtUtc\",o.\"OrderNumber\",o.\"CurrencyCode\",i.\"ProductNameSnapshot\" FROM \"ReturnRequests\" r JOIN \"Orders\" o ON o.\"Id\"=r.\"OrderId\" JOIN \"OrderItems\" i ON i.\"Id\"=r.\"OrderItemId\" " + (orderId.HasValue ? "WHERE r.\"OrderId\"=@order " : "") + "ORDER BY r.\"CreatedAtUtc\" DESC";
            if (orderId.HasValue) { var p = command.CreateParameter(); p.ParameterName = "order"; p.Value = orderId.Value; command.Parameters.Add(p); }
            using var reader = await command.ExecuteReaderAsync(); var rows = new List<ReturnEntry>();
            while (await reader.ReadAsync()) rows.Add(new(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetString(4), reader.GetString(5), reader.GetInt32(6) == 1, decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture), decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture), reader.GetString(9), reader.GetString(10), reader.GetInt32(11), reader.GetString(12), reader.GetString(13), reader.GetString(14), reader.GetString(15)));
            return rows;
        }
        finally { await _db.Database.CloseConnectionAsync(); }
    }

    public async Task<ReturnOrder> OrderAsync(int id, bool reload, CancellationToken ct)
    {
        Order order;
        if (reload) { order = (await _db.Orders.FindAsync(new object[] { id }, ct))!; await _db.Entry(order).ReloadAsync(ct); }
        else order = await _db.Orders.Include(o => o.Customer).SingleAsync(o => o.Id == id, ct);
        return new(order.Id, order.Status == OrderStatus.Delivered, order.StockDeducted, order.Total, order.ShippingTotal, order.Subtotal, order.OrderNumber, order.CurrencyCode, order.Customer?.Email, order.GuestEmail);
    }
    public async Task<ReturnLine?> LineAsync(int order, int item, CancellationToken ct)
    {
        var i = await _db.OrderItems.SingleOrDefaultAsync(i => i.Id == item && i.OrderId == order, ct); return i is null ? null : new(i.Id, i.ProductId, i.VariantId, i.Quantity, i.LineTotal);
    }
    public Task InsertAsync(string id, int order, int item, int quantity, string reason, decimal max, DateTime now, CancellationToken ct) => _db.Database.ExecuteSqlRawAsync("INSERT INTO \"ReturnRequests\" VALUES ({0},{1},{2},{3},{4},'Requested',0,{5},'0.00','','',1,{6})", new object[] { id, order, item, quantity, reason, Money(max), now.ToString("O") }, ct);
    public async Task<bool> UpdateAsync(ReturnEntry entry, int version, string next, string notes, bool restock, string action, decimal refund, string reference, CancellationToken ct) => await _db.Database.ExecuteSqlRawAsync("UPDATE \"ReturnRequests\" SET \"State\"={0},\"Notes\"={1},\"Version\"=\"Version\"+1,\"Restocked\"={2},\"RefundAmount\"={3},\"RefundReference\"={4} WHERE \"Id\"={5} AND \"Version\"={6}", new object[] { next, notes, entry.Restocked || restock ? 1 : 0, action == "RecordRefund" ? Money(refund) : Money(entry.RefundAmount), action == "RecordRefund" ? reference : entry.RefundReference, entry.Id, version }, ct) == 1;
    public async Task<bool> RestockAsync(ReturnLine item, int quantity, CancellationToken ct)
    {
        int restored = item.VariantId.HasValue ? await _db.ProductVariants.Where(v => v.Id == item.VariantId && v.ProductId == item.ProductId && v.StockQuantity <= int.MaxValue - quantity).ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + quantity), ct) : await _db.Products.Where(p => p.Id == item.ProductId && !p.HasVariants && p.StockQuantity <= int.MaxValue - quantity).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity), ct);
        return restored == 1;
    }
    public Task AuditAsync(string id, string action, string actor, string notes, DateTime now, CancellationToken ct) => _db.Database.ExecuteSqlRawAsync("INSERT INTO \"ReturnEvents\" VALUES ({0},{1},{2},{3},{4},{5})", new object[] { Guid.NewGuid().ToString("N"), id, action, actor, notes, now.ToString("O") }, ct);
    public Task QueueAsync(string to, string subject, string body, CancellationToken ct) => _mail.EnqueueAsync(to, subject, body);
    private sealed class Transaction : IReturnTransaction
    {
        private readonly IDbContextTransaction _transaction; public Transaction(IDbContextTransaction transaction) => _transaction = transaction;
        public Task CommitAsync(CancellationToken ct) => _transaction.CommitAsync(ct); public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
