using Microsoft.AspNetCore.Identity;
using Shop.DAL.Models;

namespace Shop.BLL.Security;

/// <summary>
/// Thin wrapper over ASP.NET Core's <see cref="PasswordHasher{TUser}"/> for
/// <see cref="AdminUser"/>. Configured with an explicit
/// <see cref="PasswordHasherOptions"/> pinned to <see cref="PasswordHasherCompatibilityMode.IdentityV3"/>
/// so the seeded constant hash stays reproducible across environments.
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    // Single shared hasher instance. AdminUser is only the generic type argument
    // (PasswordHasher<T> does not read the user object for the default algorithm).
    // CompatibilityMode is pinned explicitly to IdentityV3 (NIT-3) so the demo
    // seed hash verifies deterministically regardless of library defaults.
    private readonly PasswordHasher<AdminUser> _hasher =
        new(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3
        }));

    /// <inheritdoc />
    public string Hash(string password) => _hasher.HashPassword(null!, password);

    /// <inheritdoc />
    public bool Verify(string storedHash, string providedPassword)
    {
        PasswordVerificationResult result =
            _hasher.VerifyHashedPassword(null!, storedHash, providedPassword);

        // Treat both Success and SuccessRehashNeeded as a valid password.
        return result != PasswordVerificationResult.Failed;
    }
}
