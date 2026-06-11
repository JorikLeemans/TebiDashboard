namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Aggregated purchased inventory for a supplier in the selected period.
/// </summary>
public sealed class SupplierPurchasedItemDto
{
    public string ItemId { get; init; } = string.Empty;

    public string ItemName { get; init; } = string.Empty;

    /// <summary>
    /// Sum of open quantities on purchase orders for this item.
    /// </summary>
    public decimal TotalQuantityOpen { get; init; }
}
