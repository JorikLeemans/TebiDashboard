using TebbiDashboard.Application.Services;

namespace TebbiDashboard.Application.Tests;

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
        var goal = new TebbiDashboard.Shared.Dtos.GoalDto
        {
            Id = "g1",
            Year = 2026,
            Period = "Q2",
            TargetValue = 1000m,
            ActualValue = 750m
        };

        var enriched = GoalCalculator.WithProgress(goal);

        Assert.Equal(75m, enriched.Percentage);
        Assert.Equal("at-risk", enriched.Status);
    }
}
