namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Admin-facing NetSuite configuration and last-sync snapshot loaded when the admin page opens.
/// Does not include secrets — only whether credentials are present and what is missing.
/// </summary>
public sealed class NetSuiteSetupStatusDto
{
    /// <summary>
    /// True when Enabled plus account, client, certificate, and private key are all set.
    /// </summary>
    public bool IsConfigured { get; init; }

    /// <summary>
    /// True when the admin Test connection action can reach NetSuite OAuth.
    /// </summary>
    public bool CanTestConnection { get; init; }

    /// <summary>
    /// True when Sync can import from NetSuite (same gate as configuration today).
    /// </summary>
    public bool CanSync { get; init; }

    /// <summary>
    /// Configuration keys that are empty. Never includes secret values.
    /// </summary>
    public IReadOnlyList<string> MissingSettings { get; init; } = [];

    /// <summary>
    /// Short explanation of configured vs missing settings.
    /// </summary>
    public string ConfigurationMessage { get; init; } = string.Empty;

    /// <summary>
    /// Explains whether dashboard pages are showing a live import or demo seed data.
    /// </summary>
    public string DataSourceMessage { get; init; } = string.Empty;

    /// <summary>
    /// Current or last background sync from this API process (in-memory; resets on restart).
    /// </summary>
    public NetSuiteSyncStatusDto LastSync { get; init; } = new();
}
