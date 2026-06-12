using Microsoft.EntityFrameworkCore;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.Data.Entities;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Goals;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// Stores goal targets in PostgreSQL/SQLite through EF Core.
/// </summary>
public sealed class EfGoalStore(TebiDashboardDbContext dbContext) : IGoalStore
{
    public async Task<IReadOnlyList<GoalDto>> UpsertTargetsAsync(
        int year,
        IReadOnlyList<GoalTargetEntryDto> targets,
        CancellationToken cancellationToken = default)
    {
        var itemNames = await LoadItemNamesAsync(targets, cancellationToken);
        var savedGoals = new List<GoalDto>();

        foreach (var target in targets)
        {
            var goalId = string.IsNullOrWhiteSpace(target.ItemId)
                ? GoalIdHelper.BuildCompanyGoalId(year, target.Period)
                : GoalIdHelper.BuildItemGoalId(year, target.ItemId!, target.Period);

            var existing = await dbContext.Goals
                .FirstOrDefaultAsync(goal => goal.Id == goalId, cancellationToken);

            if (existing is null)
            {
                existing = new GoalRecord
                {
                    Id = goalId,
                    Year = year,
                    Period = target.Period,
                    Type = "Revenue",
                    ItemId = target.ItemId,
                    ItemName = target.ItemId is null
                        ? null
                        : itemNames.GetValueOrDefault(target.ItemId, target.ItemId),
                    TargetValue = target.TargetValue,
                    ActualValue = 0m,
                    Status = "on-track"
                };

                dbContext.Goals.Add(existing);
            }
            else
            {
                existing.TargetValue = target.TargetValue;
                existing.Period = target.Period;
                existing.Year = year;

                if (!string.IsNullOrWhiteSpace(target.ItemId))
                {
                    existing.ItemId = target.ItemId;
                    existing.ItemName = itemNames.GetValueOrDefault(target.ItemId, target.ItemId);
                }
            }

            savedGoals.Add(MapGoal(existing));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return savedGoals;
    }

    private async Task<Dictionary<string, string>> LoadItemNamesAsync(
        IReadOnlyList<GoalTargetEntryDto> targets,
        CancellationToken cancellationToken)
    {
        var itemIds = targets
            .Where(target => !string.IsNullOrWhiteSpace(target.ItemId))
            .Select(target => target.ItemId!)
            .Distinct()
            .ToList();

        if (itemIds.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return await dbContext.InventoryItems
            .AsNoTracking()
            .Where(item => itemIds.Contains(item.ItemId))
            .ToDictionaryAsync(item => item.ItemId, item => item.ItemName, cancellationToken);
    }

    private static GoalDto MapGoal(GoalRecord goal) => new()
    {
        Id = goal.Id,
        Year = goal.Year,
        Period = goal.Period,
        Type = goal.Type,
        ItemId = goal.ItemId,
        ItemName = goal.ItemName,
        TargetValue = goal.TargetValue,
        ActualValue = goal.ActualValue,
        Percentage = 0,
        Status = goal.Status
    };
}
