namespace TebiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Singleton row that remembers the last NetSuite sync across API restarts.
/// </summary>
public sealed class NetSuiteSyncCheckpointRecord
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>When the last successful import finished.</summary>
    public DateTimeOffset? LastSucceededAt { get; set; }

    /// <summary>When the last run finished, success or failure.</summary>
    public DateTimeOffset? LastCompletedAt { get; set; }

    /// <summary>Lookback days used on the last run (0 if none has completed).</summary>
    public int LastLookbackDays { get; set; }

    public string LastState { get; set; } = string.Empty;

    public string LastMessage { get; set; } = string.Empty;
}
