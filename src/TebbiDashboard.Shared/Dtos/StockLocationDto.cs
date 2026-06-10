namespace TebbiDashboard.Shared.Dtos;

/// <summary>
/// Stock quantity at a specific warehouse or yard location.
/// </summary>
public sealed class StockLocationDto
{
    public string LocationId { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public decimal QuantityOnHand { get; init; }
}
