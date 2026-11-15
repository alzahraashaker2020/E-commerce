using Shop.Application.Provisioning;
using Xunit;
namespace Shop.Application.Tests.Provisioning;

public class ProvisioningServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stage_failure_marks_failed_without_activating_partial_store(bool failActivation)
    {
        var store = new Store { FailActivation = failActivation }; var result = await new StoreProvisioningService(store).CreateAsync(new(), "operator");
        Assert.Equal(ProvisioningOutcomeKind.Failed, result.Kind); Assert.True(store.Failed); Assert.Equal(failActivation, store.ActivationAttempted);
    }
    [Fact]
    public async Task Preflight_failure_never_registers_or_initializes()
    {
        var store = new Store { Invalid = true }; var result = await new StoreProvisioningService(store).CreateAsync(new(), "operator"); Assert.Equal(ProvisioningOutcomeKind.Conflict, result.Kind); Assert.False(store.Registered); Assert.False(store.Failed);
    }
    private sealed class Store : IPlatformRegistrationStore
    {
        public bool FailActivation, Invalid, Registered, Failed, ActivationAttempted;
        public Task<ApprovalPreparation> PrepareApprovalAsync(ApprovalCommand c) => throw new NotSupportedException();
        public Task<ProvisioningOutcome?> ValidateAsync(ProvisioningRequest r) => Task.FromResult<ProvisioningOutcome?>(Invalid ? new(ProvisioningOutcomeKind.Conflict) : null);
        public Task<ProvisioningOutcome?> RegisterAsync(ProvisioningRequest r) { Registered = true; return Task.FromResult<ProvisioningOutcome?>(null); }
        public Task InitializeTenantAsync(ProvisioningRequest r) => FailActivation ? Task.CompletedTask : Task.FromException(new InvalidOperationException("Injected init failure"));
        public Task<ProvisionedStore> ActivateAsync(string a, string p) { ActivationAttempted = true; return Task.FromException<ProvisionedStore>(new InvalidOperationException("Injected platform save failure")); }
        public Task FailAsync() { Failed = true; return Task.CompletedTask; }
        public Task ResetApprovalIfProvisioningAsync() => Task.CompletedTask;
    }
}
