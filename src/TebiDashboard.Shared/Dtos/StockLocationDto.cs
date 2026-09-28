namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Stock quantity at a specific warehouse or yard location.
/// </summary>
public sealed class StockLocationDto
{
    public string LocationId { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public decimal QuantityOnHand { get; init; }

    /// <summary>Reorder trigger quantity at this location (itemlocationconfiguration.reorderpoint).</summary>
    public decimal ReorderPoint { get; init; }

    /// <summary>Target stock after replenishment (itemlocationconfiguration.preferredstocklevel).</summary>
    public decimal PreferredStockLevel { get; init; }

    /// <summary>True when this location has a reorder point and on-hand is at or below it.</summary>
    public bool IsLowStock => ReorderPoint > 0 && QuantityOnHand <= ReorderPoint;
}
