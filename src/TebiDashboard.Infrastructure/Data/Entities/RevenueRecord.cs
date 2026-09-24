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
    /// Human-readable product group (NetSuite subsidiary/class) name shown in the UI.
    /// The numeric <see cref="ProductGroupId"/> is kept as a fallback for older rows that
    /// were synced before the name was persisted.
    /// </summary>
    public string ProductGroupName { get; set; } = string.Empty;
}
