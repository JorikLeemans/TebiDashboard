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
    /// Product group name for display (artikelgroep). Empty when only the NetSuite id is known;
    /// callers then show <see cref="ProductGroupId"/>.
    /// </summary>
    public string? ProductGroupName { get; init; }
}
