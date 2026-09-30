using TebiDashboard.Shared.Goals;

namespace TebiDashboard.Application.Tests;

public class ItemVolumeYearTotalTests
{
    [Fact]
    public void Resolve_SumsQuarters_WhenAnyQuarterHasAValue()
    {
        var targets = new Dictionary<string, decimal?>
        {
            [GoalPeriods.Q1] = 100m,
            [GoalPeriods.Q2] = null,
            [GoalPeriods.Q3] = 50m,
            [GoalPeriods.Q4] = 0m,
            [GoalPeriods.Year] = 999m
        };

        Assert.Equal(150m, ItemVolumeYearTotal.Resolve(targets));
        Assert.True(ItemVolumeYearTotal.HasQuarterValue(targets));
    }

    [Fact]
    public void Resolve_KeepsYearTotal_WhenEveryQuarterIsEmpty()
    {
        var targets = new Dictionary<string, decimal?>
        {
            [GoalPeriods.Q1] = null,
            [GoalPeriods.Q2] = 0m,
            [GoalPeriods.Q3] = null,
            [GoalPeriods.Q4] = null,
            [GoalPeriods.Year] = 800m
        };

        Assert.Equal(800m, ItemVolumeYearTotal.Resolve(targets));
        Assert.False(ItemVolumeYearTotal.HasQuarterValue(targets));
    }
}
