namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Inventory item with stock level information.
/// </summary>
public sealed class InventoryItemDto
{
    public string ItemId { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public decimal QuantityOnHand { get; init; }
    public decimal? ReorderPoint { get; init; }
    /// <summary>
    /// Invoice amount for the selected period on the inventory list.
    /// Low-stock results still use the quantity stored from the last NetSuite sync.
    /// </summary>
    public decimal RecentSalesVolume { get; init; }
    public bool IsLowStock { get; init; }
}
