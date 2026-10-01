using System.Globalization;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Reports;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Filters;
using TebiDashboard.Shared.Periods;

namespace TebiDashboard.Application.Services;

/// <summary>
/// Loads the same period overview the home page uses, plus the company goal for that quarter.
/// </summary>
public sealed class PeriodReportService(IDashboardService dashboardService) : IPeriodReportService
{
    private static readonly CultureInfo DutchCulture = CultureInfo.GetCultureInfo("nl-NL");

    public async Task<PeriodReport> BuildAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var overview = await dashboardService.GetPeriodOverviewAsync(filter, cancellationToken);
        var quarter = PeriodHelper.GetQuarter(filter.To);
        var goals = await dashboardService.GetGoalsWithProgressAsync(filter.To.Year, quarter, cancellationToken);
        var companyGoal = goals.FirstOrDefault(goal => string.IsNullOrWhiteSpace(goal.ItemId));

        return new PeriodReport
        {
            Overview = overview,
            GoalPeriodLabel = $"{quarter} {filter.To.Year}",
            GoalPercentage = companyGoal?.Percentage ?? overview.Summary.GoalPercentage,
            RevenueTrend = DescribeRevenueTrend(overview)
        };
    }

    /// <summary>
    /// Matches the revenue card: a percentage versus the previous window, otherwise the summary word.
    /// </summary>
    private static string DescribeRevenueTrend(PeriodOverviewDto overview)
    {
        var points = overview.Trend;
        if (points.Count < 2)
        {
            return overview.Summary.RevenueTrend;
        }

        var current = points[^1];
        var previous = points[^2];
        var change = PeriodTrendWindows.DescribeChange(current.Revenue, previous.Revenue);
        return $"{change} vs {FormatWindow(previous)}";
    }

    private static string FormatWindow(PeriodTrendPointDto point)
    {
        const string pattern = "dd-MM-yyyy";
        return point.From == point.To
            ? point.From.ToString(pattern, DutchCulture)
            : $"{point.From.ToString(pattern, DutchCulture)} – {point.To.ToString(pattern, DutchCulture)}";
    }
}
