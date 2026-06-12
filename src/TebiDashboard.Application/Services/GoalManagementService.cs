using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Goals;

namespace TebiDashboard.Application.Services;

/// <summary>
/// Validates goal settings before they are stored.
/// </summary>
public sealed class GoalManagementService(IGoalStore goalStore) : IGoalManagementService
{
    public async Task<IReadOnlyList<GoalDto>> SaveGoalsAsync(
        SaveGoalsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Year < 2000 || request.Year > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Year), "Year must be between 2000 and 2100.");
        }

        var normalizedTargets = request.Targets
            .Where(target => target.TargetValue > 0)
            .Select(NormalizeTarget)
            .ToList();

        if (normalizedTargets.Count == 0)
        {
            throw new ArgumentException("At least one target value greater than zero is required.");
        }

        var savedGoals = await goalStore.UpsertTargetsAsync(request.Year, normalizedTargets, cancellationToken);
        return savedGoals.Select(goal => GoalCalculator.WithProgress(goal)).ToList();
    }

    private static GoalTargetEntryDto NormalizeTarget(GoalTargetEntryDto target)
    {
        var period = target.Period.Trim().ToUpperInvariant();
        if (string.Equals(period, "YEAR", StringComparison.Ordinal))
        {
            period = GoalPeriods.Year;
        }

        if (!GoalPeriods.IsSupported(period))
        {
            throw new ArgumentException($"Unsupported goal period '{target.Period}'.");
        }

        return new GoalTargetEntryDto
        {
            Period = period,
            ItemId = string.IsNullOrWhiteSpace(target.ItemId) ? null : target.ItemId.Trim(),
            TargetValue = target.TargetValue
        };
    }
}
