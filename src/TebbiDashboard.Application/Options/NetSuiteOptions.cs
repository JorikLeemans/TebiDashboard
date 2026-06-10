namespace TebbiDashboard.Application.Options;

/// <summary>
/// NetSuite connection settings bound from configuration.
/// </summary>
public sealed class NetSuiteOptions
{
    public const string SectionName = "NetSuite";

    public bool UseMock { get; set; } = true;
    public string AccountId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}
