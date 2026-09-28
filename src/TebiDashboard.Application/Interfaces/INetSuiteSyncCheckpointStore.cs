namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Last completed NetSuite sync stored in the dashboard database so restarts keep the timestamp.
/// </summary>
public sealed record NetSuiteSyncCheckpoint(
    DateTimeOffset? LastSucceededAt,
    DateTimeOffset? LastCompletedAt,
    int LastLookbackDays,
    string LastState,
    string LastMessage);

/// <summary>
/// Reads and writes the singleton NetSuite sync checkpoint.
/// </summary>
public interface INetSuiteSyncCheckpointStore
{
    Task<NetSuiteSyncCheckpoint> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(NetSuiteSyncCheckpoint checkpoint, CancellationToken cancellationToken = default);
}
