using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Application.Orders;
using Shop.BLL.Email;
using Shop.DAL.Data;
using Shop.DAL.Models;
namespace Shop.Infrastructure.Orders;

public sealed class EfOrderFulfillmentStore : IOrderFulfillmentStore
{
    private readonly ShopContext _db; private readonly IEmailService _email;
    public EfOrderFulfillmentStore(ShopContext db, IEmailService email) { _db = db; _email = email; }
    public async Task<IOrderFulfillmentSession> OpenAsync(CancellationToken cancellationToken) => new Session(_db, _email, await _db.Database.BeginTransactionAsync(cancellationToken));
    private sealed class Session : IOrderFulfillmentSession
    {
        private readonly ShopContext _db; private readonly IEmailService _email; private readonly IDbContextTransaction _transaction;
        private Order? _order; private OrderStatusDefinition? _definition;
        public Session(ShopContext db, IEmailService email, IDbContextTransaction transaction) { _db = db; _email = email; _transaction = transaction; }
        private static FulfillmentStatus ToStatus(OrderStatus value) => Enum.Parse<FulfillmentStatus>(value.ToString());
        public async Task<FulfillmentOrder?> LoadOrderAsync(int id, CancellationToken ct)
        {
            _order = await _db.Orders.Include(o => o.Items).Include(o => o.StatusDefinition).FirstOrDefaultAsync(o => o.Id == id, ct);
            return _order is null ? null : new(_order.Id, ToStatus(_order.Status), _order.StatusDefinitionId, _order.StockDeducted, _order.Items.Select(i => new FulfillmentLine(i.ProductId, i.VariantId, i.Quantity)).ToArray());
        }
        public async Task<FulfillmentDefinition?> FindDefinitionAsync(int? id, FulfillmentStatus? legacy, CancellationToken ct)
        {
            OrderStatus? status = legacy.HasValue ? Enum.Parse<OrderStatus>(legacy.Value.ToString()) : null;
            _definition = id.HasValue ? await _db.OrderStatusDefinitions.FirstOrDefaultAsync(s => s.Id == id && s.IsActive, ct) : await _db.OrderStatusDefinitions.Where(s => s.Behavior == status && s.IsActive).OrderBy(s => s.Id).FirstOrDefaultAsync(ct);
            return _definition is null ? null : new(_definition.Id, ToStatus(_definition.Behavior));
        }
        public async Task<bool> AdjustStockAsync(FulfillmentLine line, bool deduct, CancellationToken ct)
        {
            int changed;
            if (line.VariantId.HasValue)
            {
                var query = _db.ProductVariants.Where(v => v.Id == line.VariantId.Value && v.ProductId == line.ProductId);
                changed = deduct ? await query.Where(v => v.IsActive && v.Product!.IsActive && v.StockQuantity >= line.Quantity).ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - line.Quantity), ct) : await query.ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + line.Quantity), ct);
            }
            else
            {
                var query = _db.Products.Where(p => p.Id == line.ProductId && !p.HasVariants);
                changed = deduct ? await query.Where(p => p.IsActive && p.StockQuantity >= line.Quantity).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - line.Quantity), ct) : await query.ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + line.Quantity), ct);
            }
            return changed == 1;
        }
        public async Task SaveAndQueueAsync(FulfillmentOrder order, FulfillmentDefinition definition, bool deducted, CancellationToken ct)
        {
            try
            {
                _order!.StockDeducted = deducted; _order.Status = _definition!.Behavior; _order.StatusDefinitionId = _definition.Id; _order.StatusDefinition = _definition;
                await _db.SaveChangesAsync(ct);
                var settings = await _db.StoreSettings.FirstOrDefaultAsync(ct) ?? new StoreSettings { CurrencyCode = "USD", CurrencySymbol = "$" };
                if (_order.CustomerId.HasValue) await _db.Entry(_order).Reference(o => o.Customer).LoadAsync(ct);
                await _email.SendStatusUpdateAsync(_order, settings);
            }
            catch (DbUpdateConcurrencyException) { throw new FulfillmentConcurrencyException(); }
        }
        public Task CommitAsync(CancellationToken ct) => _transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
