namespace TebiDashboard.Api.Authorization;

/// <summary>
/// Resolves the HMAC key used to sign and validate access tokens.
/// The development fallback is refused outside Development so a deployed API cannot start with a public key.
/// </summary>
public static class AuthSigningKey
{
    /// <summary>Known development key. Tokens signed with it are only accepted in Development.</summary>
    public const string DevelopmentFallback = "tebi-dashboard-dev-signing-key-not-for-production";

    public const int MinimumLength = 32;

    public static string Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Auth:SigningKey"];
        if (IsUsableSecret(configured))
        {
            return configured!;
        }

        if (environment.IsDevelopment())
        {
            return DevelopmentFallback;
        }

        throw new InvalidOperationException(
            "Auth:SigningKey must be set to a secret of at least 32 characters outside Development.");
    }

    private static bool IsUsableSecret(string? configured) =>
        !string.IsNullOrWhiteSpace(configured)
        && configured.Length >= MinimumLength
        && !string.Equals(configured, DevelopmentFallback, StringComparison.Ordinal);
}
