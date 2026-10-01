using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Application.Reports;

/// <summary>
/// Figures for one dashboard period, ready to write into Excel or PDF.
/// Goal progress follows the home page: the company goal for the quarter that contains the period end.
/// </summary>
public sealed class PeriodReport
{
    public required PeriodOverviewDto Overview { get; init; }

    /// <summary>Quarter label shown next to goal progress, for example Q4 2026.</summary>
    public string GoalPeriodLabel { get; init; } = string.Empty;

    public decimal GoalPercentage { get; init; }

    /// <summary>Change versus the previous equal-length window, or the summary word when that window is missing.</summary>
    public string RevenueTrend { get; init; } = string.Empty;
}
