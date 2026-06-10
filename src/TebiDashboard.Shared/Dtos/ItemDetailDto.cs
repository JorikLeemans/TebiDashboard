namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Full item detail returned after barcode lookup.
/// </summary>
public sealed class ItemDetailDto
{
    public string ItemId { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public decimal TotalQuantityOnHand { get; init; }
    public decimal? ReorderPoint { get; init; }
    public decimal RecentSalesVolume { get; init; }
    public bool IsLowStock { get; init; }
    public IReadOnlyList<StockLocationDto> Locations { get; init; } = [];
    public int OpenTransactionCount { get; init; }
    public IReadOnlyList<OpenTransactionDto> OpenTransactions { get; init; } = [];
}
