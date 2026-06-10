using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Shared.Dtos;
using TebbiDashboard.Shared.Filters;

namespace TebbiDashboard.Application.Services;

/// <summary>
/// Orchestrates dashboard queries via the NetSuite gateway.
/// </summary>
public sealed class DashboardService(INetSuiteService netSuiteService) : IDashboardService
{
    public Task<DashboardSummaryDto> GetSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
        => netSuiteService.GetDashboardSummaryAsync(filter, cancellationToken);

    public Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
        => netSuiteService.GetRevenueAsync(filter, cancellationToken);

    public async Task<IReadOnlyList<GoalDto>> GetGoalsWithProgressAsync(int year, string? period = null, CancellationToken cancellationToken = default)
    {
        var goals = await netSuiteService.GetGoalsAsync(year, period, cancellationToken);
        return goals.Select(GoalCalculator.WithProgress).ToList();
    }
}
