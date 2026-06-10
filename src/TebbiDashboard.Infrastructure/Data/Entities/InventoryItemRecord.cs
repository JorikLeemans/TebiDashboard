namespace TebbiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Inventory item with stock and sales metrics.
/// </summary>
public sealed class InventoryItemRecord
{
    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal ReorderPoint { get; set; }

    public decimal RecentSalesVolume { get; set; }

    public bool IsLowStock { get; set; }
}
