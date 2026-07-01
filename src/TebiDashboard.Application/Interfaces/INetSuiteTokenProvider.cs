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
    /// True when account ID, client ID, certificate ID, and private key are configured.
    /// </summary>
    bool IsConfigured { get; }
}
