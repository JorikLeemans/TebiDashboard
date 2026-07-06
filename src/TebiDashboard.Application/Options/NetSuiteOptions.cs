namespace TebiDashboard.Application.Options;

/// <summary>
/// NetSuite OAuth 2.0 connection settings bound from configuration and user secrets.
/// </summary>
public sealed class NetSuiteOptions
{
    public const string SectionName = "NetSuite";

    /// <summary>
    /// When true, NetSuite token and REST helpers are registered (still uses demo DB for dashboard data until sync exists).
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// NetSuite account ID used in the token and REST base URL (e.g. 1234567 or 1234567_SB1).
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth 2.0 client ID from the integration record (Consumer Key).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth 2.0 client secret from the integration record. Stored for future flows; M2M uses the certificate JWT.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Certificate ID (kid) returned after uploading the public certificate in M2M setup.
    /// </summary>
    public string CertificateId { get; set; } = string.Empty;

    /// <summary>
    /// Absolute or relative path to the RSA private key PEM used to sign the client assertion JWT.
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// Inline PEM private key for hosted environments (e.g. Railway multiline variable NetSuite__PrivateKeyPem).
    /// Takes precedence over <see cref="PrivateKeyPath"/> when set.
    /// </summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// OAuth scopes requested in the client assertion JWT.
    /// </summary>
    public string[] Scopes { get; set; } = ["rest_webservices"];

    /// <summary>
    /// How many days of invoices, bills, and open orders to pull during sync.
    /// </summary>
    public int SyncLookbackDays { get; set; } = 365;

    /// <summary>
    /// SuiteQL page size used while paging through NetSuite result sets.
    /// </summary>
    public int SyncPageSize { get; set; } = 200;

    /// <summary>
    /// When true, dashboard tables are cleared before importing NetSuite data (users and goals are kept).
    /// </summary>
    public bool ClearDashboardDataOnSync { get; set; } = true;

    /// <summary>
    /// HTTP timeout (minutes) for NetSuite REST calls and the admin sync API request from the UI.
    /// </summary>
    public int SyncHttpTimeoutMinutes { get; set; } = 30;

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(CertificateId)
        && HasPrivateKey;

    /// <summary>
    /// True when either inline PEM content or a key file path is configured.
    /// </summary>
    public bool HasPrivateKey =>
        !string.IsNullOrWhiteSpace(PrivateKeyPem) || !string.IsNullOrWhiteSpace(PrivateKeyPath);

    /// <summary>
    /// Lists missing settings so admins can fix configuration without exposing secrets.
    /// </summary>
    public IReadOnlyList<string> GetMissingConfigurationKeys()
    {
        var missing = new List<string>();
        if (!Enabled)
        {
            missing.Add("Enabled");
        }

        if (string.IsNullOrWhiteSpace(AccountId))
        {
            missing.Add("AccountId");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            missing.Add("ClientId");
        }

        if (string.IsNullOrWhiteSpace(CertificateId))
        {
            missing.Add("CertificateId");
        }

        if (!HasPrivateKey)
        {
            missing.Add("PrivateKeyPem or PrivateKeyPath");
        }

        return missing;
    }

    public string DescribeMissingConfiguration() =>
        GetMissingConfigurationKeys().Count == 0
            ? string.Empty
            : $"Missing NetSuite settings: {string.Join(", ", GetMissingConfigurationKeys())}.";
}
