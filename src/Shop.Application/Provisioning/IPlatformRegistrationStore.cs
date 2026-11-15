namespace Shop.Application.Provisioning;

public interface IStoreProvisioner { Task InitializeTenantAsync(ProvisioningRequest request); }
public interface IPlatformRegistrationStore : IStoreProvisioner
{
    Task<ApprovalPreparation> PrepareApprovalAsync(ApprovalCommand command);
    Task<ProvisioningOutcome?> ValidateAsync(ProvisioningRequest request);
    Task<ProvisioningOutcome?> RegisterAsync(ProvisioningRequest request);
    Task<ProvisionedStore> ActivateAsync(string actor, string paymentReference);
    Task FailAsync();
    Task ResetApprovalIfProvisioningAsync();
}
