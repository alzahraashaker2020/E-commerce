using Shop.Application.Accounts;
using Xunit;
namespace Shop.Application.Tests.Accounts;

public class RecoveryPolicyTests
{
    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task Forgot_normalizes_email_but_only_issues_for_password_accounts(bool known, bool password, int issues)
    {
        var store = new Store { Customer = known ? new(7, password) : null }; await new CustomerRecoveryService(store).ForgotAsync("  USER@EXAMPLE.TEST ");
        Assert.Equal("user@example.test", store.Email); Assert.Equal(issues, store.Issues);
    }
    private sealed class Store : ICustomerRecoveryStore
    {
        public RecoveryCustomer? Customer; public string? Email; public int Issues;
        public Task<RecoveryCustomer?> FindAsync(string email) { Email = email; return Task.FromResult(Customer); }
        public Task IssueAsync(int id, string purpose) { Assert.Equal(7, id); Assert.Equal("ResetPassword", purpose); Issues++; return Task.CompletedTask; }
        public Task<bool> ConsumeAsync(string token, string purpose, string? password) => Task.FromResult(false);
        public Task<bool> IsVerifiedAsync(int id) => Task.FromResult(false);
    }
}
