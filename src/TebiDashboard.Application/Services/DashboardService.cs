using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Application.Services;

/// <summary>
/// Orchestrates dashboard queries via the NetSuite gateway.
/// </summary>
public sealed class DashboardService(INetSuiteService netSuiteService) : IDashboardService
{
    public Task<DashboardSummaryDto> GetSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
        => netSuiteService.GetDashboardSummaryAsync(filter, cancellationToken);

    public Task<PeriodOverviewDto> GetPeriodOverviewAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
        => netSuiteService.GetPeriodOverviewAsync(filter, cancellationToken);

    public Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
        => netSuiteService.GetRevenueAsync(filter, cancellationToken);

    public async Task<IReadOnlyList<GoalDto>> GetGoalsWithProgressAsync(int year, string? period = null, CancellationToken cancellationToken = default)
    {
        var goals = await netSuiteService.GetGoalsAsync(year, period, cancellationToken);
        return goals.Select(GoalCalculator.WithProgress).ToList();
    }
}
