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

    /// <summary>
    /// Updates the in-progress step so the admin page can show a progress bar.
    /// </summary>
    void ReportProgress(string currentStep, int completedSteps, int totalSteps);
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

    /// <summary>Human-readable step name shown on the admin progress bar.</summary>
    public string CurrentStep { get; init; } = string.Empty;

    /// <summary>Finished steps in this run.</summary>
    public int CompletedSteps { get; init; }

    /// <summary>Total steps planned for this run.</summary>
    public int TotalSteps { get; init; }
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
