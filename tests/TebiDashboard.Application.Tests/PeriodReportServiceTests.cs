using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Services;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Application.Tests;

/// <summary>
/// The export snapshot uses the company goal for the quarter of the period end, like the home page.
/// </summary>
public class PeriodReportServiceTests
{
    [Fact]
    public async Task Build_UsesTheCompanyGoal_ForTheQuarterOfThePeriodEnd()
    {
        var service = new PeriodReportService(new FakeDashboardService
        {
            Overview = new PeriodOverviewDto
            {
                PeriodFrom = new DateOnly(2026, 10, 1),
                PeriodTo = new DateOnly(2026, 10, 1),
                Summary = new DashboardSummaryDto { GoalPercentage = 10m, RevenueTrend = "up" }
            },
            Goals =
            [
                new GoalDto { ItemId = "i1", Percentage = 5m },
                new GoalDto { ItemId = null, Percentage = 42m }
            ]
        });

        var report = await service.BuildAsync(new DateRangeFilter
        {
            From = new DateOnly(2026, 10, 1),
            To = new DateOnly(2026, 10, 1)
        });

        Assert.Equal("Q4 2026", report.GoalPeriodLabel);
        Assert.Equal(42m, report.GoalPercentage);
        Assert.Equal("up", report.RevenueTrend);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public PeriodOverviewDto Overview { get; init; } = new();

        public IReadOnlyList<GoalDto> Goals { get; init; } = [];

        public Task<DashboardSummaryDto> GetSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PeriodOverviewDto> GetPeriodOverviewAsync(DateRangeFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(Overview);

        public Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<GoalDto>> GetGoalsWithProgressAsync(int year, string? period = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Goals);
    }
}
