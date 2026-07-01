namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Verifies that OAuth 2.0 credentials can obtain a NetSuite access token.
/// </summary>
public interface INetSuiteConnectionService
{
    Task<NetSuiteConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a NetSuite connectivity check.
/// </summary>
public sealed record NetSuiteConnectionResult(
    bool IsConfigured,
    bool IsConnected,
    string Message,
    string? TokenPreview = null);
