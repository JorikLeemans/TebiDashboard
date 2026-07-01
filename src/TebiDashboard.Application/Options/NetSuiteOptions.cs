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
    /// OAuth scopes requested in the client assertion JWT.
    /// </summary>
    public string[] Scopes { get; set; } = ["rest_webservices"];

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(CertificateId)
        && !string.IsNullOrWhiteSpace(PrivateKeyPath);
}
