namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// API response summarizing a NetSuite dashboard data import.
/// </summary>
public sealed class NetSuiteSyncResultDto
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public int CustomersSynced { get; init; }

    public int SuppliersSynced { get; init; }

    public int InventoryItemsSynced { get; init; }

    public int StockLocationsSynced { get; init; }

    public int RevenueRecordsSynced { get; init; }

    public int ExpenseRecordsSynced { get; init; }

    public int OpenTransactionsSynced { get; init; }

    public DateTimeOffset CompletedAt { get; init; }
}
