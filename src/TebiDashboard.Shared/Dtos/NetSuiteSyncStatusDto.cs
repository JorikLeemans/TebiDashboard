namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Background NetSuite sync progress returned by GET /api/netsuite/sync/status.
/// </summary>
public sealed class NetSuiteSyncStatusDto
{
    /// <summary>Idle, Running, Succeeded, or Failed.</summary>
    public string State { get; init; } = "Idle";

    public string Message { get; init; } = string.Empty;

    public NetSuiteSyncResultDto? Result { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }
}
