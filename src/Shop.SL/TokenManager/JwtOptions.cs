namespace Shop.SL.TokenManager;

/// <summary>Shared signing and validation settings. Values never belong in diagnostics.</summary>
public sealed class JwtOptions
{
    public string SecretKey { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 120;
}
