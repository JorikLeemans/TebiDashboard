namespace TebiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Revenue line persisted for dashboard reporting.
/// </summary>
public sealed class RevenueRecord
{
    public string Id { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public decimal Amount { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string ProductGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable product group (artikelgroep) name shown in the UI.
    /// <see cref="ProductGroupId"/> stays the NetSuite internal id. When this is empty the UI
    /// falls back to that id so older rows still render.
    /// </summary>
    public string ProductGroupName { get; set; } = string.Empty;
}
