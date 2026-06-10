namespace TebbiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Operating expense persisted for dashboard cost views.
/// </summary>
public sealed class ExpenseRecord
{
    public string Id { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string Category { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string VendorName { get; set; } = string.Empty;
}
