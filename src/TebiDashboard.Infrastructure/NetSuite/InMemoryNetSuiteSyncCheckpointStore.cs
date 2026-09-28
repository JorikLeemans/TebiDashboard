using TebiDashboard.Application.Interfaces;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// In-memory checkpoint used by the API test host (no PostgreSQL).
/// </summary>
public sealed class InMemoryNetSuiteSyncCheckpointStore : INetSuiteSyncCheckpointStore
{
    private readonly object _gate = new();
    private NetSuiteSyncCheckpoint _checkpoint = new(null, null, 0, string.Empty, string.Empty);

    public Task<NetSuiteSyncCheckpoint> GetAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_checkpoint);
        }
    }

    public Task SaveAsync(NetSuiteSyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _checkpoint = checkpoint;
        }

        return Task.CompletedTask;
    }
}
