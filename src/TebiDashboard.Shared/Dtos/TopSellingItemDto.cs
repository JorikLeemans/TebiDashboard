namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// An item ranked by invoice amount in the selected period.
/// </summary>
public sealed class TopSellingItemDto
{
    public string ItemId { get; init; } = string.Empty;

    public string ItemName { get; init; } = string.Empty;

    public string ProductGroupId { get; init; } = string.Empty;

    public string ProductGroupName { get; init; } = string.Empty;

    /// <summary>Sum of invoice line amounts for this item in the selected period.</summary>
    public decimal SalesAmount { get; init; }

    /// <summary>Sum of invoice line quantities for this item in the selected period.</summary>
    public decimal UnitsSold { get; init; }
}
