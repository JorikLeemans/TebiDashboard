namespace TebiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Stock quantity for an item at a warehouse or yard location.
/// </summary>
public sealed class StockLocationRecord
{
    public int Id { get; set; }

    public string ItemId { get; set; } = string.Empty;

    public string LocationId { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    /// <summary>
    /// NetSuite itemlocationconfiguration.reorderpoint for this item at this location.
    /// </summary>
    public decimal ReorderPoint { get; set; }

    /// <summary>
    /// NetSuite itemlocationconfiguration.preferredstocklevel for this item at this location.
    /// </summary>
    public decimal PreferredStockLevel { get; set; }
}
