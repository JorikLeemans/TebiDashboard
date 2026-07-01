namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// API response from the NetSuite OAuth connectivity check.
/// </summary>
public sealed class NetSuiteConnectionResultDto
{
    public bool IsConfigured { get; init; }

    public bool IsConnected { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? TokenPreview { get; init; }
}
