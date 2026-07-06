namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Runs NetSuite dashboard imports in the background so HTTP requests can finish quickly.
/// </summary>
public interface INetSuiteSyncCoordinator
{
    /// <summary>
    /// Starts a background sync when none is running.
    /// </summary>
    bool TryStart();

    NetSuiteSyncRunStatus GetStatus();
}

/// <summary>
/// In-memory status of the current or last NetSuite sync run.
/// </summary>
public sealed class NetSuiteSyncRunStatus
{
    public string State { get; init; } = NetSuiteSyncStates.Idle;

    public string Message { get; init; } = string.Empty;

    public NetSuiteSyncResult? Result { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>
/// Known background sync state values.
/// </summary>
public static class NetSuiteSyncStates
{
    public const string Idle = "Idle";

    public const string Running = "Running";

    public const string Succeeded = "Succeeded";

    public const string Failed = "Failed";
}
