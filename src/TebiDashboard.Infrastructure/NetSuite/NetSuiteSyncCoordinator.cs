using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Constants;

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
    private int _activeLookbackDays = NetSuiteLookback.DefaultDays;

    public bool TryStart(int lookbackDays)
    {
        lookbackDays = NetSuiteLookback.Clamp(lookbackDays);

        lock (_gate)
        {
            if (_status.State == NetSuiteSyncStates.Running)
            {
                return false;
            }

            _activeLookbackDays = lookbackDays;
            _status = new NetSuiteSyncRunStatus
            {
                State = NetSuiteSyncStates.Running,
                Message = $"NetSuite sync started ({lookbackDays}-day lookback).",
                StartedAt = DateTimeOffset.UtcNow,
                CurrentStep = "starting",
                CompletedSteps = 0,
                // Default import has eight steps; the service overwrites this if tables are cleared first.
                TotalSteps = 8,
                LookbackDays = lookbackDays
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

    public void ReportProgress(string currentStep, int completedSteps, int totalSteps)
    {
        lock (_gate)
        {
            // Drop late reports after Succeeded/Failed so a finished run is not overwritten.
            if (_status.State != NetSuiteSyncStates.Running)
            {
                return;
            }

            _status = new NetSuiteSyncRunStatus
            {
                State = NetSuiteSyncStates.Running,
                Message = $"Importing {currentStep}…",
                StartedAt = _status.StartedAt,
                CurrentStep = currentStep,
                CompletedSteps = completedSteps,
                TotalSteps = totalSteps,
                LookbackDays = _status.LookbackDays
            };
        }
    }

    private async Task RunSyncAsync()
    {
        var lookbackDays = _activeLookbackDays;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<INetSuiteSyncService>();
            var result = await syncService.SyncDashboardDataAsync(lookbackDays);
            var completedAt = DateTimeOffset.UtcNow;
            var state = result.Success ? NetSuiteSyncStates.Succeeded : NetSuiteSyncStates.Failed;

            lock (_gate)
            {
                _status = new NetSuiteSyncRunStatus
                {
                    State = state,
                    Message = result.Message,
                    Result = result,
                    StartedAt = _status.StartedAt,
                    CompletedAt = completedAt,
                    LookbackDays = lookbackDays
                };
            }

            var store = scope.ServiceProvider.GetRequiredService<INetSuiteSyncCheckpointStore>();
            var previous = await store.GetAsync();
            await store.SaveAsync(new NetSuiteSyncCheckpoint(
                result.Success ? completedAt : previous.LastSucceededAt,
                completedAt,
                lookbackDays,
                state,
                result.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background NetSuite sync failed.");
            var completedAt = DateTimeOffset.UtcNow;
            lock (_gate)
            {
                _status = new NetSuiteSyncRunStatus
                {
                    State = NetSuiteSyncStates.Failed,
                    Message = ex.Message,
                    StartedAt = _status.StartedAt,
                    CompletedAt = completedAt,
                    LookbackDays = lookbackDays
                };
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<INetSuiteSyncCheckpointStore>();
                var previous = await store.GetAsync();
                await store.SaveAsync(new NetSuiteSyncCheckpoint(
                    previous.LastSucceededAt,
                    completedAt,
                    lookbackDays,
                    NetSuiteSyncStates.Failed,
                    ex.Message));
            }
            catch (Exception saveEx)
            {
                logger.LogWarning(saveEx, "Could not persist NetSuite sync checkpoint after a failure.");
            }
        }
    }
}
