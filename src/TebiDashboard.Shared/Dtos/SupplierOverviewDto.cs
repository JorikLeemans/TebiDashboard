namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Supplier summary for the suppliers list, including spend and activity in the selected period.
/// </summary>
public sealed class SupplierOverviewDto
{
    public string SupplierId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string ContactEmail { get; init; } = string.Empty;

    /// <summary>
    /// Total expense amount attributed to this supplier within the selected period.
    /// </summary>
    public decimal TotalPurchased { get; init; }

    public int ExpenseEntryCount { get; init; }

    public int PurchaseOrderCount { get; init; }

    public int PurchasedItemCount { get; init; }
}
