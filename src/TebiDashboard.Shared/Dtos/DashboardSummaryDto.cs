namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// KPI summary shown on the dashboard home page.
/// </summary>
public sealed class DashboardSummaryDto
{
    public decimal Revenue { get; init; }
    public decimal SalesVolume { get; init; }
    public decimal GoalPercentage { get; init; }
    public string RevenueTrend { get; init; } = "stable";
    public int LowStockAlertCount { get; init; }
    public DateOnly PeriodFrom { get; init; }
    public DateOnly PeriodTo { get; init; }
}
