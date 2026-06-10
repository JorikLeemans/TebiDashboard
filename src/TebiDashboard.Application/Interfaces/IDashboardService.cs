using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Application service for dashboard use cases.
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoalDto>> GetGoalsWithProgressAsync(int year, string? period = null, CancellationToken cancellationToken = default);
}
