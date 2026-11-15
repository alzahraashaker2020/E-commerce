using Shop.DAL.Data;

namespace Shop.Api.Tenancy;

public sealed class StoreScope : IStoreIdentity
{
    public StoreRegistration? Store { get; set; }
    public StoreSubscription? Subscription { get; set; }
    public string StoreId => Store?.Id ?? throw new InvalidOperationException("No store resolved for this operation.");
    public string SecurityVersion { get; set; } = "";
}
