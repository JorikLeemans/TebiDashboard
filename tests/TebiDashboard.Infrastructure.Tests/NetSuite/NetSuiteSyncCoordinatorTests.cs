using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Infrastructure.NetSuite;
using TebiDashboard.Shared.Constants;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Progress updates are only stored while a background sync is Running.
/// </summary>
public sealed class NetSuiteSyncCoordinatorTests
{
    [Fact]
    public void ReportProgress_IsIgnored_WhenIdle()
    {
        var coordinator = CreateCoordinator(out _);

        coordinator.ReportProgress("invoices", 4, 8);

        var status = coordinator.GetStatus();
        Assert.Equal(NetSuiteSyncStates.Idle, status.State);
        Assert.Equal(string.Empty, status.CurrentStep);
        Assert.Equal(0, status.CompletedSteps);
    }

    [Fact]
    public void ReportProgress_UpdatesCurrentStep_WhileRunning()
    {
        var coordinator = CreateCoordinator(out var hanging);
        Assert.True(coordinator.TryStart(14));

        coordinator.ReportProgress("invoices", 4, 8);

        var status = coordinator.GetStatus();
        Assert.Equal(NetSuiteSyncStates.Running, status.State);
        Assert.Equal("invoices", status.CurrentStep);
        Assert.Equal(4, status.CompletedSteps);
        Assert.Equal(8, status.TotalSteps);
        Assert.Equal(14, status.LookbackDays);
        Assert.Contains("invoices", status.Message, StringComparison.OrdinalIgnoreCase);

        hanging.Complete();
    }

    [Fact]
    public void TryStart_ClampsLookbackDays()
    {
        var coordinator = CreateCoordinator(out var hanging);
        Assert.True(coordinator.TryStart(10_000));
        Assert.Equal(NetSuiteLookback.MaxDays, coordinator.GetStatus().LookbackDays);
        hanging.Complete();
    }

    private static NetSuiteSyncCoordinator CreateCoordinator(out HangingSyncService hanging)
    {
        hanging = new HangingSyncService();
        var services = new ServiceCollection();
        services.AddSingleton<INetSuiteSyncService>(hanging);
        services.AddSingleton<INetSuiteSyncCheckpointStore, InMemoryNetSuiteSyncCheckpointStore>();
        var provider = services.BuildServiceProvider();
        return new NetSuiteSyncCoordinator(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NetSuiteSyncCoordinator>.Instance);
    }

    /// <summary>
    /// Keeps the coordinator in Running until the test finishes asserting progress.
    /// </summary>
    private sealed class HangingSyncService : INetSuiteSyncService
    {
        private readonly TaskCompletionSource<NetSuiteSyncResult> _completion = new();

        public int? LastLookbackDays { get; private set; }

        public Task<NetSuiteSyncResult> SyncDashboardDataAsync(int lookbackDays, CancellationToken cancellationToken = default)
        {
            LastLookbackDays = lookbackDays;
            return _completion.Task;
        }

        public void Complete() =>
            _completion.TrySetResult(new NetSuiteSyncResult(
                Success: true,
                Message: "done",
                CustomersSynced: 0,
                SuppliersSynced: 0,
                InventoryItemsSynced: 0,
                StockLocationsSynced: 0,
                RevenueRecordsSynced: 0,
                ExpenseRecordsSynced: 0,
                OpenTransactionsSynced: 0,
                CompletedAt: DateTimeOffset.UtcNow));
    }
}
