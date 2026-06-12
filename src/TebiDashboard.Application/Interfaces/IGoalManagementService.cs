using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Persists revenue and item goal targets configured in the UI.
/// </summary>
public interface IGoalManagementService
{
    Task<IReadOnlyList<GoalDto>> SaveGoalsAsync(
        SaveGoalsRequestDto request,
        CancellationToken cancellationToken = default);
}
