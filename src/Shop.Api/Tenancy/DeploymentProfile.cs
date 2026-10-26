using Microsoft.EntityFrameworkCore;

namespace Shop.Api.Tenancy;

/// <summary>Operator-owned installation settings. Never supplied by a browser or merchant token.</summary>
public sealed class DeploymentProfile
{
    public string Mode { get; }
    public bool SaasEnabled => Mode == "Saas";
    public string StandaloneStoreId { get; }
    public string? BusinessModeOverride { get; }

    public DeploymentProfile(IConfiguration config)
    {
        Mode = config["Deployment:Mode"] ?? "Saas";
        if (Mode is not ("Saas" or "Standalone"))
            throw new InvalidOperationException("Deployment:Mode must be Saas or Standalone.");
        StandaloneStoreId = config["Deployment:StandaloneStoreId"] ?? "default";
        if (string.IsNullOrWhiteSpace(StandaloneStoreId) || StandaloneStoreId.Length > 80)
            throw new InvalidOperationException("Configure a valid standalone store ID.");
        var businessMode = config["Deployment:BusinessMode"];
        BusinessModeOverride = string.IsNullOrWhiteSpace(businessMode) ? null : businessMode;
        if (BusinessModeOverride is not (null or "Commerce" or "Catalog"))
            throw new InvalidOperationException("Deployment:BusinessMode must be Commerce or Catalog, or omitted for the store setting.");
    }

    public IQueryable<StoreRegistration> Stores(PlatformContext registry) => SaasEnabled
        ? registry.Stores
        : registry.Stores.Where(s => s.Id == StandaloneStoreId);
}
