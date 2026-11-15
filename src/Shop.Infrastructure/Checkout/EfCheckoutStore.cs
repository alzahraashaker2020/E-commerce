using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Application.Checkout;
using Shop.BLL.Email;
using Shop.DAL.Data;
using Shop.DAL.Models;
namespace Shop.Infrastructure.Checkout;

public sealed class EfCheckoutStore : ICheckoutStore
{
    private readonly ShopContext _db; private readonly IEmailService _email;
    public EfCheckoutStore(ShopContext db, IEmailService email) { _db = db; _email = email; }
    public async Task<CheckoutCustomer?> CustomerAsync(int id, CancellationToken ct) { var c = await _db.Customers.FindAsync(new object[] { id }, ct); return c is null ? null : new(c.Id, c.FullName, c.Email); }
    public async Task<ICheckoutSession> OpenAsync(CancellationToken ct) { await _db.Database.OpenConnectionAsync(ct); return new Session(_db); }
    public async Task<CheckoutProduct?> ProductAsync(int id, bool activeOnly, CancellationToken ct)
    {
        var query = _db.Products.Include(p => p.Variants).ThenInclude(v => v.Size).Include(p => p.Variants).ThenInclude(v => v.Color).AsQueryable();
        if (activeOnly) query = query.AsNoTracking().Where(p => p.IsActive);
        var p = await query.FirstOrDefaultAsync(p => p.Id == id, ct);
        return p is null ? null : new(p.Id, p.Name, p.IsActive, p.HasVariants, p.StockQuantity, p.BasePrice, p.Variants.Select(v => new CheckoutVariant(v.Id, v.IsActive, v.StockQuantity, v.Price, v.Size is null ? null : new(v.Size.Name), v.Color is null ? null : new(v.Color.Name))).ToArray());
    }
    public async Task<CheckoutSettings?> SettingsAsync(bool required, CancellationToken ct)
    {
        var s = required ? await _db.StoreSettings.FirstAsync(ct) : await _db.StoreSettings.FirstOrDefaultAsync(ct);
        return s is null ? null : new(s.CurrencyCode, s.CurrencySymbol);
    }
    public async Task<CheckoutArea?> AreaAsync(int id, string currency, CancellationToken ct)
    {
        var a = await _db.DeliveryAreas.AsNoTracking().Include(a => a.Governorate).FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.Governorate!.IsActive && a.CurrencyCode == currency, ct);
        return a is null ? null : new(a.Id, a.Name, a.ShippingFee, a.Governorate is null ? null : new(a.Governorate.Name));
    }
    public async Task<DiscountSnapshot?> DiscountAsync(string code, CancellationToken ct)
    {
        var d = await _db.Discounts.FirstOrDefaultAsync(d => d.Code == code, ct); return d is null ? null : new(d.IsActive, d.StartsAt, d.EndsAt, d.Type == DiscountType.Percentage, d.Value);
    }
    public async Task<InitialStatus> InitialStatusAsync(CancellationToken ct) { var s = await _db.OrderStatusDefinitions.FirstAsync(s => s.IsDefault && s.IsActive, ct); return new(s.Id, s.Name); }
    public async Task SaveOrderAsync(CheckoutOrder draft, CancellationToken ct)
    {
        var order = new Order
        {
            CustomerId = draft.CustomerId,
            GuestName = draft.GuestName,
            GuestEmail = draft.GuestEmail,
            Status = OrderStatus.Pending,
            StatusDefinitionId = draft.StatusDefinitionId,
            Subtotal = draft.Subtotal,
            DiscountTotal = draft.DiscountTotal,
            Tax = draft.Tax,
            ShippingTotal = draft.ShippingTotal,
            DeliveryAreaId = draft.DeliveryAreaId,
            Total = draft.Total,
            CurrencyCode = draft.CurrencyCode,
            PaymentMethod = PaymentMethod.Cash,
            PaymentStatus = PaymentStatus.CashOnDelivery,
            AddressLine1 = draft.AddressLine1,
            AddressLine2 = draft.AddressLine2,
            City = draft.City,
            StateRegion = draft.StateRegion,
            PostalCode = draft.PostalCode,
            Country = draft.Country,
            ContactPhone = draft.ContactPhone,
            PlacedAt = draft.PlacedAt,
            IsDemoData = draft.IsDemoData,
            OrderNumber = draft.OrderNumber,
            Items = draft.Items.Select(i => new OrderItem { ProductId = i.ProductId, VariantId = i.VariantId, ProductNameSnapshot = i.ProductNameSnapshot, SizeSnapshot = i.SizeSnapshot, ColorSnapshot = i.ColorSnapshot, UnitPrice = i.UnitPrice, Quantity = i.Quantity, LineTotal = i.LineTotal, IsDemoData = i.IsDemoData }).ToList()
        };
        _db.Orders.Add(order); await Duplicate(async () => { await _db.SaveChangesAsync(ct); });
    }
    public Task QueueSummaryAsync(string number, CancellationToken ct) => Duplicate(async () =>
    {
        var order = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.OrderNumber == number, ct);
        var settings = await _db.StoreSettings.FirstAsync(ct); await _email.SendOrderSummaryAsync(order, settings);
    });
    private static async Task Duplicate(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) when (SqlServerSupport.Duplicate(ex)) { throw new CheckoutDuplicateException(); }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { throw new CheckoutDuplicateException(); }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505") { throw new CheckoutDuplicateException(); }
    }
    private sealed class Session : ICheckoutSession
    {
        private readonly ShopContext _db; private IDbContextTransaction? _transaction;
        public Session(ShopContext db) => _db = db;
        public async Task<ReceiptSnapshot?> ReceiptAsync(string key, CancellationToken ct)
        {
            using var command = _db.Database.GetDbConnection().CreateCommand(); command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT \"Owner\",\"RequestHash\",\"ResponseJson\" FROM \"CheckoutReceipts\" WHERE \"Key\"=@key";
            var parameter = command.CreateParameter(); parameter.ParameterName = "key"; parameter.Value = key; command.Parameters.Add(parameter);
            using var reader = await command.ExecuteReaderAsync(ct); return await reader.ReadAsync(ct) ? new(reader.GetString(0), reader.GetString(1), reader.GetString(2)) : null;
        }
        public async Task BeginAsync(CancellationToken ct) => _transaction = await _db.Database.BeginTransactionAsync(ct);
        public Task WriteReceiptAsync(string key, string owner, string hash, string json, CancellationToken ct) => Duplicate(() => _db.Database.ExecuteSqlRawAsync("INSERT INTO \"CheckoutReceipts\" (\"Key\",\"Owner\",\"RequestHash\",\"ResponseJson\") VALUES ({0},{1},{2},{3})", new object[] { key, owner, hash, json }, ct));
        public Task CommitAsync(CancellationToken ct) => Duplicate(() => _transaction!.CommitAsync(ct));
        public async Task RollbackAsync(CancellationToken ct) { await _transaction!.RollbackAsync(ct); _db.ChangeTracker.Clear(); }
        public async ValueTask DisposeAsync() { try { if (_transaction is not null) await _transaction.DisposeAsync(); } finally { await _db.Database.CloseConnectionAsync(); } }
    }
}
