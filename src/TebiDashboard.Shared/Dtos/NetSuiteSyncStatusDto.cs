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

    /// <summary>Sync step currently running, for example "invoices".</summary>
    public string CurrentStep { get; init; } = string.Empty;

    /// <summary>Finished steps in this run (0 while the first step is in progress).</summary>
    public int CompletedSteps { get; init; }

    /// <summary>Total steps in this run. 0 when no run is in progress.</summary>
    public int TotalSteps { get; init; }
}
