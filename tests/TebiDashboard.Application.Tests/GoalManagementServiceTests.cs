using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Services;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Goals;

namespace TebiDashboard.Application.Tests;

public class GoalManagementServiceTests
{
    [Fact]
    public async Task SaveGoalsAsync_RejectsEmptyTargets()
    {
        var service = new GoalManagementService(new FakeGoalStore());

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveGoalsAsync(new SaveGoalsRequestDto
        {
            Year = 2026,
            Targets = []
        }));
    }

    [Fact]
    public async Task SaveGoalsAsync_NormalizesYearPeriod()
    {
        var store = new FakeGoalStore();
        var service = new GoalManagementService(store);

        var saved = await service.SaveGoalsAsync(new SaveGoalsRequestDto
        {
            Year = 2026,
            Targets =
            [
                new GoalTargetEntryDto { Period = GoalPeriods.Year, TargetValue = 2_000_000m }
            ]
        });

        Assert.Single(saved);
        Assert.Equal(GoalPeriods.Year, saved[0].Period);
        Assert.Equal("g-2026-year", saved[0].Id);
    }

    private sealed class FakeGoalStore : IGoalStore
    {
        public Task<IReadOnlyList<GoalDto>> UpsertTargetsAsync(
            int year,
            IReadOnlyList<GoalTargetEntryDto> targets,
            CancellationToken cancellationToken = default)
        {
            var goals = targets.Select(target => new GoalDto
            {
                Id = string.IsNullOrWhiteSpace(target.ItemId)
                    ? GoalIdHelper.BuildCompanyGoalId(year, target.Period)
                    : GoalIdHelper.BuildItemGoalId(year, target.ItemId!, target.Period),
                Year = year,
                Period = target.Period,
                TargetValue = target.TargetValue,
                ActualValue = 0m
            }).ToList();

            return Task.FromResult<IReadOnlyList<GoalDto>>(goals);
        }
    }
}
