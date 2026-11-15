using Microsoft.AspNetCore.DataProtection;
using Shop.Infrastructure.Notifications;
using Xunit;
namespace Shop.RegressionTests;

public class EncryptionContractTests
{
    [Fact]
    public void Existing_purpose_and_key_ring_remain_readable_after_adapter_change()
    {
        var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "shop-protection-" + Guid.NewGuid().ToString("N")));
        var oldProvider = DataProtectionProvider.Create(directory); var old = oldProvider.CreateProtector("StoreEmailOutbox", "alpha"); var existing = old.Protect("private payload");
        var next = DataProtectionProvider.Create(directory).CreateProtector("StoreEmailOutbox", "alpha"); var adapter = new EmailPayloadProtector(next.Protect, next.Unprotect);
        Assert.Equal("private payload", adapter.Unprotect(existing)); Assert.Equal("new payload", old.Unprotect(adapter.Protect("new payload")));
        var other = oldProvider.CreateProtector("StoreEmailOutbox", "beta"); Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => other.Unprotect(existing));
    }
}
