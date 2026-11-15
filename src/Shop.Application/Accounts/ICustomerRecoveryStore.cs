namespace Shop.Application.Accounts;

public interface ICustomerRecoveryStore
{
    Task<RecoveryCustomer?> FindAsync(string email);
    Task IssueAsync(int customerId, string purpose);
    Task<bool> ConsumeAsync(string token, string purpose, string? password);
    Task<bool> IsVerifiedAsync(int customerId);
}
