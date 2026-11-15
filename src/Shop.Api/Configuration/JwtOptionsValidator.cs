using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Shop.SL.TokenManager;

namespace Shop.Api.Configuration;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.SecretKey) || Encoding.UTF8.GetByteCount(options.SecretKey) < 32 ||
            options.SecretKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
            options.SecretKey.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase) ||
            options.SecretKey.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
            options.SecretKey.Contains("dev_only", StringComparison.OrdinalIgnoreCase) ||
            options.SecretKey.Contains("do_not_use_in_production", StringComparison.OrdinalIgnoreCase))
            failures.Add("Jwt:SecretKey must be a private signing key of at least 32 UTF-8 bytes, not a development placeholder. Supply it through local secrets or Jwt__SecretKey.");
        if (string.IsNullOrWhiteSpace(options.Issuer)) failures.Add("Jwt:Issuer is required.");
        if (string.IsNullOrWhiteSpace(options.Audience)) failures.Add("Jwt:Audience is required.");
        if (options.ExpiryMinutes <= 0) failures.Add("Jwt:ExpiryMinutes must be a positive 32-bit integer.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    // Read and validate before any filesystem/database initialization. Avoid binder errors
    // that could echo an invalid secret-like configuration value into startup diagnostics.
    public static JwtOptions ReadValidated(IConfiguration configuration)
    {
        string? expiry = configuration["Jwt:ExpiryMinutes"];
        int minutes = 120; // Preserve the existing omitted-expiry default.
        if (expiry is not null && !int.TryParse(expiry, NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes))
            throw new OptionsValidationException("Jwt", typeof(JwtOptions), new[] { "Jwt:ExpiryMinutes must be a positive 32-bit integer." });
        var options = new JwtOptions
        {
            SecretKey = configuration["Jwt:SecretKey"] ?? "",
            Issuer = configuration["Jwt:Issuer"] ?? "",
            Audience = configuration["Jwt:Audience"] ?? "",
            ExpiryMinutes = minutes
        };
        var result = new JwtOptionsValidator().Validate("Jwt", options);
        if (result.Failed) throw new OptionsValidationException("Jwt", typeof(JwtOptions), result.Failures);
        return options;
    }
}
