namespace TebbiDashboard.Infrastructure.Data.Entities;

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
}
