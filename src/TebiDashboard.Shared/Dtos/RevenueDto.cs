namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Revenue data point for charts and tables.
/// </summary>
public sealed class RevenueDto
{
    public string Id { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? ProductGroupId { get; init; }

    /// <summary>
    /// Human-readable product group name for display. Falls back to <see cref="ProductGroupId"/>
    /// in the UI when this is empty (e.g. rows synced before the name was captured).
    /// </summary>
    public string? ProductGroupName { get; init; }
}
