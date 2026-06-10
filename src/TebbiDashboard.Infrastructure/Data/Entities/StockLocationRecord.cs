namespace TebbiDashboard.Infrastructure.Data.Entities;

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
}
