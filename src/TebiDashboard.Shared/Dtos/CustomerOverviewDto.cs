namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Customer summary for the customers list, including spend and activity in the selected period.
/// </summary>
public sealed class CustomerOverviewDto
{
    public string CustomerId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string ContactEmail { get; init; } = string.Empty;

    /// <summary>
    /// Customer category label from NetSuite, not the internal list id.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Optional TEBI klant subcategory label from custentity_tebi_klant_subcategorie.
    /// </summary>
    public string Subcategory { get; init; } = string.Empty;

    /// <summary>
    /// Total revenue attributed to this customer within the selected period.
    /// </summary>
    public decimal TotalSpent { get; init; }

    public int SalesOrderCount { get; init; }

    public int RevenueEntryCount { get; init; }
}
