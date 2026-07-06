using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TebiDashboard.Application.Interfaces;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Executes <see cref="INetSuiteSyncService"/> on a background thread and tracks progress for polling clients.
/// </summary>
public sealed class NetSuiteSyncCoordinator(
    IServiceScopeFactory scopeFactory,
    ILogger<NetSuiteSyncCoordinator> logger) : INetSuiteSyncCoordinator
{
    private readonly object _gate = new();
    private NetSuiteSyncRunStatus _status = new();

    public bool TryStart()
    {
        lock (_gate)
        {
            if (_status.State == NetSuiteSyncStates.Running)
            {
                return false;
            }

            _status = new NetSuiteSyncRunStatus
            {
                State = NetSuiteSyncStates.Running,
                Message = "NetSuite sync started.",
                StartedAt = DateTimeOffset.UtcNow
            };
        }

        _ = Task.Run(RunSyncAsync);
        return true;
    }

    public NetSuiteSyncRunStatus GetStatus()
    {
        lock (_gate)
        {
            return _status;
        }
    }

    private async Task RunSyncAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<INetSuiteSyncService>();
            var result = await syncService.SyncDashboardDataAsync();

            lock (_gate)
            {
                _status = new NetSuiteSyncRunStatus
                {
                    State = result.Success ? NetSuiteSyncStates.Succeeded : NetSuiteSyncStates.Failed,
                    Message = result.Message,
                    Result = result,
                    StartedAt = _status.StartedAt,
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background NetSuite sync failed.");
            lock (_gate)
            {
                _status = new NetSuiteSyncRunStatus
                {
                    State = NetSuiteSyncStates.Failed,
                    Message = ex.Message,
                    StartedAt = _status.StartedAt,
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }
        }
    }
}
