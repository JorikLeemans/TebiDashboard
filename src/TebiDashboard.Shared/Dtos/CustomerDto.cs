namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Customer master data for dashboard and sales order views.
/// </summary>
public sealed class CustomerDto
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
}
