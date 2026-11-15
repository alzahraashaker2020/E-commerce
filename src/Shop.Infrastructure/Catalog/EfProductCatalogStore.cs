using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Application.Catalog;
using Shop.DAL.Data;
using Shop.DAL.Models;
namespace Shop.Infrastructure.Catalog;

public sealed class EfProductCatalogStore : IProductCatalogStore
{
    private readonly ShopContext _db; private readonly Dictionary<int, Product> _loaded = new();
    public EfProductCatalogStore(ShopContext db) => _db = db;
    public async Task<IProductCreateTransaction> BeginCreateAsync(CancellationToken ct) => new Transaction(await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct));
    public Task<int> CountAsync(CancellationToken ct) => _db.Products.CountAsync(ct);
    public async Task<CatalogProduct?> LoadAsync(int id, CancellationToken ct) { var p = await _db.Products.Include(p => p.Variants).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, ct); if (p is null) return null; _loaded[p.Id] = p; return Snapshot(p); }
    public Task<bool> HasOrderReferenceAsync(int id, CancellationToken ct) => _db.OrderItems.AnyAsync(i => i.VariantId == id, ct);
    public Task<bool> HasCartReferenceAsync(int id, CancellationToken ct) => _db.CartItems.AnyAsync(i => i.VariantId == id, ct);
    private static CatalogProduct Snapshot(Product p)
    {
        var result = new CatalogProduct { Id = p.Id, Name = p.Name, Slug = p.Slug, DescriptionHtml = p.DescriptionHtml, Brand = p.Brand, BasePrice = p.BasePrice, CategoryId = p.CategoryId, HasVariants = p.HasVariants, ShowStockQuantity = p.ShowStockQuantity, StockQuantity = p.StockQuantity, LowStockThreshold = p.LowStockThreshold, IsActive = p.IsActive, CreatedAt = p.CreatedAt, IsDemoData = p.IsDemoData };
        result.Variants = p.Variants.Select(v => new CatalogVariant { Id = v.Id, ProductId = v.ProductId, SizeId = v.SizeId, ColorId = v.ColorId, Sku = v.Sku, Price = v.Price, StockQuantity = v.StockQuantity, LowStockThreshold = v.LowStockThreshold, IsActive = v.IsActive, ImageUrl = v.ImageUrl, ImageUrls = v.ImageUrls.ToList(), IsDemoData = v.IsDemoData }).ToList();
        result.Images = p.Images.Select(i => new CatalogImage { Id = i.Id, ProductId = i.ProductId, Url = i.Url, AltText = i.AltText, SortOrder = i.SortOrder, IsDemoData = i.IsDemoData }).ToList(); return result;
    }
    private Product Apply(CatalogProduct draft)
    {
        bool created = draft.Id == 0; var p = created ? new Product() : _loaded[draft.Id];
        _db.Entry(p).CurrentValues.SetValues(draft);
        foreach (var old in p.Variants.Where(v => !draft.Variants.Any(d => d.Id != 0 && d.Id == v.Id)).ToList()) p.Variants.Remove(old);
        foreach (var row in draft.Variants) { var v = row.Id == 0 ? null : p.Variants.Single(v => v.Id == row.Id); if (v is null) { v = new ProductVariant(); p.Variants.Add(v); } _db.Entry(v).CurrentValues.SetValues(row); }
        foreach (var old in p.Images.Where(i => !draft.Images.Any(d => d.Id != 0 && d.Id == i.Id)).ToList()) p.Images.Remove(old);
        foreach (var row in draft.Images) { var i = row.Id == 0 ? null : p.Images.Single(i => i.Id == row.Id); if (i is null) { i = new ProductImage(); p.Images.Add(i); } _db.Entry(i).CurrentValues.SetValues(row); }
        if (created) _db.Products.Add(p); else _db.Products.Update(p); return p;
    }
    public int Save(CatalogProduct draft)
    {
        var p = Apply(draft); try { _db.SaveChanges(); } catch (Exception ex) when (SqlServerSupport.SerializationFailure(ex)) { throw new ProductCapacityException(); } catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "40001" }) { throw new ProductCapacityException(); } catch (Npgsql.PostgresException ex) when (ex.SqlState == "40001") { throw new ProductCapacityException(); }
        draft.Id = p.Id; return p.Id;
    }
    public async Task<bool> DeleteAsync(int id, CancellationToken ct) { var p = await _db.Products.FindAsync(new object[] { id }, ct); if (p is null) return false; _db.Products.Remove(p); _db.SaveChanges(); return true; }
    public async Task<List<CatalogProduct>> LoadManyAsync(List<int> ids, CancellationToken ct)
    {
        var rows = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct); foreach (var p in rows) _loaded[p.Id] = p; return rows.Select(Snapshot).ToList();
    }
    public void SaveMany(List<CatalogProduct> products) { foreach (var p in products) Apply(p); _db.SaveChanges(); }
    private sealed class Transaction : IProductCreateTransaction
    {
        private readonly IDbContextTransaction _transaction; public Transaction(IDbContextTransaction transaction) => _transaction = transaction;
        public async Task CommitAsync(CancellationToken ct) { try { await _transaction.CommitAsync(ct); } catch (Npgsql.PostgresException ex) when (ex.SqlState == "40001") { throw new ProductCapacityException(); } catch (Exception ex) when (SqlServerSupport.SerializationFailure(ex)) { throw new ProductCapacityException(); } }
        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}

