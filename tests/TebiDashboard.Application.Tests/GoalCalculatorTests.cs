using TebiDashboard.Application.Services;

namespace TebiDashboard.Application.Tests;

public class GoalCalculatorTests
{
    [Fact]
    public void ComputePercentage_ReturnsZero_WhenTargetIsZero()
    {
        var result = GoalCalculator.ComputePercentage(100m, 0m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ComputePercentage_ReturnsCorrectValue_WhenTargetIsPositive()
    {
        var result = GoalCalculator.ComputePercentage(875m, 1000m);
        Assert.Equal(87.5m, result);
    }

    [Fact]
    public void ComputeStatus_ReturnsOnTrack_WhenPercentageAbove90()
    {
        var status = GoalCalculator.ComputeStatus(95m);
        Assert.Equal("on-track", status);
    }

    [Fact]
    public void WithProgress_EnrichesGoal_WithPercentageAndStatus()
    {
        var goal = new TebiDashboard.Shared.Dtos.GoalDto
        {
            Id = "g1",
            Year = 2026,
            Period = "Q2",
            TargetValue = 1000m,
            ActualValue = 750m
        };

        var enriched = GoalCalculator.WithProgress(goal, new DateOnly(2026, 6, 15));

        Assert.Equal(75m, enriched.Percentage);
        Assert.Equal("at-risk", enriched.Status);
    }

    [Fact]
    public void WithProgress_ReturnsAchieved_WhenCompletedPeriodMeetsTarget()
    {
        var goal = new TebiDashboard.Shared.Dtos.GoalDto
        {
            Id = "g1",
            Year = 2026,
            Period = "Q1",
            TargetValue = 480_000m,
            ActualValue = 500_000m
        };

        var enriched = GoalCalculator.WithProgress(goal, new DateOnly(2026, 6, 15));

        Assert.Equal("achieved", enriched.Status);
    }

    [Fact]
    public void WithProgress_ReturnsNotAchieved_WhenCompletedPeriodMissesTarget()
    {
        var goal = new TebiDashboard.Shared.Dtos.GoalDto
        {
            Id = "g1",
            Year = 2025,
            Period = "Q2",
            TargetValue = 480_000m,
            ActualValue = 462_000m
        };

        var enriched = GoalCalculator.WithProgress(goal, new DateOnly(2026, 6, 15));

        Assert.Equal("not-achieved", enriched.Status);
    }
}
