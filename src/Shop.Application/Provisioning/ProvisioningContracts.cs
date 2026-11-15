namespace Shop.Application.Provisioning;

public sealed class ProvisioningRequest
{
    public string Name { get; set; } = ""; public string Host { get; set; } = ""; public string ConnectionKey { get; set; } = ""; public string OwnerUsername { get; set; } = ""; public string OwnerEmail { get; set; } = ""; public string OwnerPassword { get; set; } = "";
}
public sealed record ApprovalCommand(string Id, int Version, string Host, string ConnectionKey, bool PaymentVerified, string PaymentReference, string Actor);
public enum ProvisioningOutcomeKind { Success, Invalid, NotFound, Conflict, Failed }
public sealed record ProvisionedStore(string Id, string Name, string Host, string State);
public sealed record ProvisioningOutcome(ProvisioningOutcomeKind Kind, string? Message = null, ProvisionedStore? Store = null, bool Bare = false);
public sealed record ApprovalPreparation(ProvisioningRequest? Request, ProvisioningOutcome? Error);
