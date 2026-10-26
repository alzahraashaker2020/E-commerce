namespace Shop.DAL.Data;

/// <summary>The resolved store for this request, never supplied as a database connection by a client.</summary>
public interface IStoreIdentity
{
    /// <summary>Stable platform registry identifier of the current store.</summary>
    string StoreId { get; }
    /// <summary>Central session generation, rotated during database recovery.</summary>
    string SecurityVersion => "";
}
