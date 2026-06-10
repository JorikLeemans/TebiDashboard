namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Aggregated dashboard data for a selected date range.
/// </summary>
public sealed class PeriodOverviewDto
{
    public DateOnly PeriodFrom { get; init; }

    public DateOnly PeriodTo { get; init; }

    public DashboardSummaryDto Summary { get; init; } = new();

    public IReadOnlyList<CustomerDto> Customers { get; init; } = [];

    public IReadOnlyList<RevenueDto> Revenue { get; init; } = [];

    public IReadOnlyList<ExpenseDto> Expenses { get; init; } = [];

    public IReadOnlyList<OpenTransactionDto> Transactions { get; init; } = [];

    public IReadOnlyList<InventoryItemDto> LowStockItems { get; init; } = [];

    public IReadOnlyList<InventoryItemDto> InventoryWithActivity { get; init; } = [];
}
