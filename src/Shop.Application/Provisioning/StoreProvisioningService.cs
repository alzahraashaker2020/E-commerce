namespace Shop.Application.Provisioning;

public sealed class StoreProvisioningService
{
    private readonly IPlatformRegistrationStore _store;
    public StoreProvisioningService(IPlatformRegistrationStore store) => _store = store;
    public async Task<ProvisioningOutcome> ApproveAsync(ApprovalCommand command)
    {
        var preparation = await _store.PrepareApprovalAsync(command);
        if (preparation.Error is not null) return preparation.Error;
        var outcome = await CreateAsync(preparation.Request!, command.Actor, command.PaymentReference);
        await _store.ResetApprovalIfProvisioningAsync(); return outcome;
    }
    public async Task<ProvisioningOutcome> CreateAsync(ProvisioningRequest request, string actor, string paymentReference = "")
    {
        var error = await _store.ValidateAsync(request); if (error is not null) return error;
        error = await _store.RegisterAsync(request); if (error is not null) return error;
        try
        {
            await _store.InitializeTenantAsync(request);
            return new(ProvisioningOutcomeKind.Success, Store: await _store.ActivateAsync(actor, paymentReference));
        }
        catch
        {
            await _store.FailAsync();
            return new(ProvisioningOutcomeKind.Failed, "Provisioning failed. Store remains unavailable; inspect server configuration before retrying.");
        }
    }
}
