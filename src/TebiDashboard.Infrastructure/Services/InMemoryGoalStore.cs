using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Goals;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// Mutable in-memory goal store shared with <see cref="MockNetSuiteService"/> in tests.
/// </summary>
public sealed class InMemoryGoalStore : IGoalStore
{
    public Task<IReadOnlyList<GoalDto>> UpsertTargetsAsync(
        int year,
        IReadOnlyList<GoalTargetEntryDto> targets,
        CancellationToken cancellationToken = default)
    {
        var savedGoals = new List<GoalDto>();

        foreach (var target in targets)
        {
            var goalId = string.IsNullOrWhiteSpace(target.ItemId)
                ? GoalIdHelper.BuildCompanyGoalId(year, target.Period)
                : GoalIdHelper.BuildItemGoalId(year, target.ItemId!, target.Period);

            var itemName = target.ItemId is null
                ? null
                : MockNetSuiteService.GetItemName(target.ItemId);

            var saved = MockGoalData.Upsert(new GoalDto
            {
                Id = goalId,
                Year = year,
                Period = target.Period,
                Type = "Revenue",
                ItemId = target.ItemId,
                ItemName = itemName,
                TargetValue = target.TargetValue,
                ActualValue = MockGoalData.Find(goalId)?.ActualValue ?? 0m,
                Percentage = 0,
                Status = "on-track"
            });

            savedGoals.Add(saved);
        }

        return Task.FromResult<IReadOnlyList<GoalDto>>(savedGoals);
    }
}

/// <summary>
/// Shared goal list used by mock services and integration tests.
/// </summary>
internal static class MockGoalData
{
    private static readonly object Sync = new();
    private static List<GoalDto> _goals = MockNetSuiteService.CreateInitialGoals();

    public static IReadOnlyList<GoalDto> GetAll()
    {
        lock (Sync)
        {
            return _goals.Select(Clone).ToList();
        }
    }

    public static GoalDto? Find(string goalId)
    {
        lock (Sync)
        {
            return _goals.FirstOrDefault(goal => goal.Id == goalId) is { } match
                ? Clone(match)
                : null;
        }
    }

    public static GoalDto Upsert(GoalDto goal)
    {
        lock (Sync)
        {
            var index = _goals.FindIndex(existing => existing.Id == goal.Id);
            if (index >= 0)
            {
                _goals[index] = goal;
            }
            else
            {
                _goals.Add(goal);
            }

            return Clone(goal);
        }
    }

    private static GoalDto Clone(GoalDto goal) => goal with { };
}
