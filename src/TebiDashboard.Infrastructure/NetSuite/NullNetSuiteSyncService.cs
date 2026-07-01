using TebiDashboard.Application.Interfaces;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// No-op sync implementation used when the dashboard database is not available (test host).
/// </summary>
public sealed class NullNetSuiteSyncService : INetSuiteSyncService
{
    public Task<NetSuiteSyncResult> SyncDashboardDataAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new NetSuiteSyncResult(
            Success: false,
            Message: "NetSuite sync requires a PostgreSQL database provider.",
            CustomersSynced: 0,
            SuppliersSynced: 0,
            InventoryItemsSynced: 0,
            StockLocationsSynced: 0,
            RevenueRecordsSynced: 0,
            ExpenseRecordsSynced: 0,
            OpenTransactionsSynced: 0,
            CompletedAt: DateTimeOffset.UtcNow));
}
