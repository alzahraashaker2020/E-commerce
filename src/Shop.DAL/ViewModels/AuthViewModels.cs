using System.ComponentModel.DataAnnotations;

namespace Shop.DAL.ViewModels;

/// <summary>
/// Admin authentication request.
/// </summary>
public class AuthenticateRequest
{
    /// <summary>Username (required, max 60 chars).</summary>
    [Required]
    [StringLength(60)]
    public string Username { get; set; } = string.Empty;

    /// <summary>Password (required, max 200 chars).</summary>
    [Required]
    [StringLength(200)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Admin authentication response carrying the issued token.
/// </summary>
public class AuthenticateResponse
{
    /// <summary>Signed JWT.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC token expiry.</summary>
    public DateTime Expires { get; set; }

    /// <summary>Authenticated username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Authenticated user's role.</summary>
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Minimal token-shaping helper returned by the token service.
/// </summary>
public class TokenVM
{
    /// <summary>Signed JWT.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC token expiry.</summary>
    public DateTime Expires { get; set; }
}
