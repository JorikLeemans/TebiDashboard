namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Pulls NetSuite records into the local dashboard database.
/// </summary>
public interface INetSuiteSyncService
{
    Task<NetSuiteSyncResult> SyncDashboardDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary of a completed NetSuite sync run.
/// </summary>
public sealed record NetSuiteSyncResult(
    bool Success,
    string Message,
    int CustomersSynced,
    int SuppliersSynced,
    int InventoryItemsSynced,
    int StockLocationsSynced,
    int RevenueRecordsSynced,
    int ExpenseRecordsSynced,
    int OpenTransactionsSynced,
    DateTimeOffset CompletedAt);
