namespace Shop.Application.Accounts;

public sealed class CustomerRecoveryService
{
    private readonly ICustomerRecoveryStore _store;
    public CustomerRecoveryService(ICustomerRecoveryStore store) => _store = store;
    public async Task ForgotAsync(string email)
    {
        var customer = await _store.FindAsync(email.Trim().ToLowerInvariant());
        if (customer is not null && customer.HasPassword) await _store.IssueAsync(customer.Id, "ResetPassword");
    }
    public Task<bool> ConsumeAsync(string token, string purpose, string? password) => _store.ConsumeAsync(token, purpose, password);
    public Task ResendAsync(int customerId) => _store.IssueAsync(customerId, "VerifyEmail");
    public Task<bool> IsVerifiedAsync(int customerId) => _store.IsVerifiedAsync(customerId);
}
