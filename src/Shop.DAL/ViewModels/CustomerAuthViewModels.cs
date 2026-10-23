using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Customer registration request (local account).
/// </summary>
public class CustomerRegisterRequest
{
    /// <summary>Full name (required, 1-200 chars).</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email (required, valid, unique).</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Password (required, 6-200 chars).</summary>
    [Required]
    [StringLength(200, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional phone number.</summary>
    [StringLength(40)]
    public string? Phone { get; set; }

    /// <summary>Whether the customer opts into marketing communications.</summary>
    public bool MarketingConsent { get; set; }
}

/// <summary>
/// Customer login request (local account).
/// </summary>
public class CustomerLoginRequest
{
    /// <summary>Account email.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Account password.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Social login request. Carries the provider name and the token/credential the
/// client obtained from the provider's SDK (validated server-side). This is the
/// plumbing shape; a real deployment verifies the token with the provider.
/// </summary>
public class SocialLoginRequest
{
    /// <summary>Provider name: "Google" or "Facebook".</summary>
    [Required]
    public string Provider { get; set; } = string.Empty;

    /// <summary>The provider-issued credential/ID token to verify server-side.</summary>
    [Required]
    public string Credential { get; set; } = string.Empty;
}

/// <summary>
/// Customer authentication response carrying the issued JWT and profile summary.
/// </summary>
public class CustomerAuthResponse
{
    /// <summary>Signed JWT.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC token expiry.</summary>
    public DateTime Expires { get; set; }

    /// <summary>Authenticated customer id.</summary>
    public int CustomerId { get; set; }

    /// <summary>Customer full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Customer email.</summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Customer profile view / update payload.
/// </summary>
public class CustomerProfileVM
{
    /// <summary>Customer id (ignored on update; taken from the token).</summary>
    public int Id { get; set; }

    /// <summary>Full name.</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email (read-only in this scaffold).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional phone number.</summary>
    [StringLength(40)]
    public string? Phone { get; set; }

    /// <summary>Marketing consent flag.</summary>
    public bool MarketingConsent { get; set; }
}
