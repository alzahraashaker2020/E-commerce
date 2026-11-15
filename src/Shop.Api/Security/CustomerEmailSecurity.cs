using Shop.Application.Accounts;
using Shop.BLL.Security;
using Shop.DAL.Models;
namespace Shop.Api.Security;
// Compatibility facade for existing registration/email verification callers.
public sealed class CustomerEmailSecurity
{
    private readonly ICustomerRecoveryStore _store;
    public CustomerEmailSecurity(ICustomerRecoveryStore store) => _store = store;
    public Task IssueAsync(Customer customer, string purpose) => _store.IssueAsync(customer.Id, purpose);
    public Task<bool> ConsumeAsync(string token, string purpose, string? password, IPasswordHasherService passwords) => _store.ConsumeAsync(token, purpose, password);
    public Task<bool> IsVerified(Customer customer) => _store.IsVerifiedAsync(customer.Id);
}
