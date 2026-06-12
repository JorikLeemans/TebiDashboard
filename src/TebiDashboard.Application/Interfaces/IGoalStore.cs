using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Low-level persistence for goal target upserts.
/// </summary>
public interface IGoalStore
{
    Task<IReadOnlyList<GoalDto>> UpsertTargetsAsync(
        int year,
        IReadOnlyList<GoalTargetEntryDto> targets,
        CancellationToken cancellationToken = default);
}
