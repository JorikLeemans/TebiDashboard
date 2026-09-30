namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Bestsellers for a period, plus every product group that had sales in that period.
/// </summary>
public sealed class TopSellingItemsResultDto
{
    public IReadOnlyList<TopSellingItemDto> Items { get; init; } = [];

    public IReadOnlyList<ProductGroupOptionDto> ProductGroups { get; init; } = [];
}
