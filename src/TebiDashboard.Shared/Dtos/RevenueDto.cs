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

    /// <summary>
    /// Inventory item that represents this revenue line's product group.
    /// Empty when the product group is not linked to an item.
    /// </summary>
    public string ItemId { get; init; } = string.Empty;

    /// <summary>
    /// Name of <see cref="ItemId"/>. When the item record is missing this is the item id;
    /// when the product group is unknown it is <see cref="ProductGroupId"/>.
    /// </summary>
    public string ItemName { get; init; } = string.Empty;

    /// <summary>
    /// NetSuite invoice internal id that this revenue line belongs to.
    /// </summary>
    public string InvoiceId { get; init; } = string.Empty;

    /// <summary>
    /// Invoice document number (tranId) shown to operators, e.g. F-2026-001.
    /// </summary>
    public string InvoiceNumber { get; init; } = string.Empty;
}
