using TebiDashboard.Application.Interfaces;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.Data.Entities;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Persists the last NetSuite sync in PostgreSQL so Admin still shows it after a restart.
/// </summary>
public sealed class EfNetSuiteSyncCheckpointStore(TebiDashboardDbContext dbContext) : INetSuiteSyncCheckpointStore
{
    public async Task<NetSuiteSyncCheckpoint> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await dbContext.NetSuiteSyncCheckpoints.FindAsync([NetSuiteSyncCheckpointRecord.SingletonId], cancellationToken);
        return row is null
            ? Empty()
            : new NetSuiteSyncCheckpoint(
                row.LastSucceededAt,
                row.LastCompletedAt,
                row.LastLookbackDays,
                row.LastState,
                row.LastMessage);
    }

    public async Task SaveAsync(NetSuiteSyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.NetSuiteSyncCheckpoints.FindAsync([NetSuiteSyncCheckpointRecord.SingletonId], cancellationToken);
        if (row is null)
        {
            row = new NetSuiteSyncCheckpointRecord { Id = NetSuiteSyncCheckpointRecord.SingletonId };
            dbContext.NetSuiteSyncCheckpoints.Add(row);
        }

        row.LastSucceededAt = checkpoint.LastSucceededAt;
        row.LastCompletedAt = checkpoint.LastCompletedAt;
        row.LastLookbackDays = checkpoint.LastLookbackDays;
        row.LastState = checkpoint.LastState;
        row.LastMessage = Truncate(checkpoint.LastMessage, 1024);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static NetSuiteSyncCheckpoint Empty() =>
        new(null, null, 0, string.Empty, string.Empty);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
