namespace Shop.BLL.Security;

/// <summary>
/// Credential hashing/verification service. Owns the single algorithm/config
/// used for admin passwords so the login verify path and the seeded hash
/// constant never diverge.
/// </summary>
public interface IPasswordHasherService
{
    /// <summary>
    /// Hashes a plaintext password for storage (used by admin-user creation and
    /// by the design-time seed-constant generator).
    /// </summary>
    /// <param name="password">The plaintext password.</param>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored hash. Returns <c>true</c>
    /// on success (including the rehash-needed result, treated as success).
    /// </summary>
    /// <param name="storedHash">The stored password hash.</param>
    /// <param name="providedPassword">The plaintext password supplied at login.</param>
    bool Verify(string storedHash, string providedPassword);
}
