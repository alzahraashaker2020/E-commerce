namespace Shop.Api.Tenancy;

public sealed class StoreCapabilities
{
    private readonly DeploymentProfile _deployment;
    private readonly StorefrontContent _content;
    public StoreCapabilities(DeploymentProfile deployment, StorefrontContent content)
    { _deployment = deployment; _content = content; }

    public async Task<string> BusinessModeAsync() => _deployment.BusinessModeOverride ?? (await _content.ReadAsync()).BusinessMode;
}
