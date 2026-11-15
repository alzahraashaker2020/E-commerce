namespace Shop.Application.Catalog;

public interface IProductCatalogStore
{
    Task<IProductCreateTransaction> BeginCreateAsync(CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
    Task<CatalogProduct?> LoadAsync(int id, CancellationToken ct);
    Task<bool> HasOrderReferenceAsync(int variant, CancellationToken ct);
    Task<bool> HasCartReferenceAsync(int variant, CancellationToken ct);
    int Save(CatalogProduct product);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<List<CatalogProduct>> LoadManyAsync(List<int> ids, CancellationToken ct);
    void SaveMany(List<CatalogProduct> products);
}
public interface IProductCreateTransaction : IAsyncDisposable { Task CommitAsync(CancellationToken ct); }
