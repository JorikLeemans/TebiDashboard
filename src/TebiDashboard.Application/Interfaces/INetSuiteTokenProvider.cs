namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Obtains OAuth 2.0 access tokens for NetSuite REST Web Services.
/// </summary>
public interface INetSuiteTokenProvider
{
    /// <summary>
    /// Returns a cached bearer token or requests a new one from NetSuite.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the cached bearer token so the next call signs a new client assertion.
    /// </summary>
    void InvalidateCachedToken();

    /// <summary>
    /// True when account ID, client ID, certificate ID, and private key are configured.
    /// </summary>
    bool IsConfigured { get; }
}
