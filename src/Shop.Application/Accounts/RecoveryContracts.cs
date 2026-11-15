namespace Shop.Application.Accounts;

public sealed record RecoveryCustomer(int Id, bool HasPassword);
public interface IAccountLinkSettings
{
    string Origin { get; }
    string StoreHost { get; }
    string StoreName { get; }
    bool AllowHttp { get; }
    bool Arabic { get; }
}
