using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Periods;

namespace TebiDashboard.Application.Services;

/// <summary>
/// Calculates goal progress percentages and status labels.
/// </summary>
public static class GoalCalculator
{
    /// <summary>
    /// Computes progress percentage; returns 0 when target is zero.
    /// </summary>
    public static decimal ComputePercentage(decimal actual, decimal target)
    {
        if (target <= 0)
        {
            return 0;
        }

        return Math.Round(actual / target * 100m, 2);
    }

    /// <summary>
    /// Maps percentage to a traffic-light status for UI display.
    /// </summary>
    public static string ComputeStatus(decimal percentage)
    {
        if (percentage >= 90)
        {
            return "on-track";
        }

        if (percentage >= 70)
        {
            return "at-risk";
        }

        return "off-track";
    }

    /// <summary>
    /// Maps a completed period to achieved or not achieved based on target vs actual.
    /// </summary>
    public static string ComputeCompletedStatus(decimal actual, decimal target) =>
        actual >= target ? "achieved" : "not-achieved";

    /// <summary>
    /// Picks traffic-light status for in-progress periods or final outcome for closed periods.
    /// </summary>
    public static string ComputeStatusForGoal(GoalDto goal, DateOnly referenceDate)
    {
        var percentage = ComputePercentage(goal.ActualValue, goal.TargetValue);

        if (PeriodHelper.IsPeriodCompleted(goal.Year, goal.Period, referenceDate))
        {
            return ComputeCompletedStatus(goal.ActualValue, goal.TargetValue);
        }

        return ComputeStatus(percentage);
    }

    /// <summary>
    /// Enriches a goal DTO with calculated percentage and status.
    /// </summary>
    public static GoalDto WithProgress(GoalDto goal, DateOnly? referenceDate = null)
    {
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var percentage = ComputePercentage(goal.ActualValue, goal.TargetValue);

        return goal with
        {
            Percentage = percentage,
            Status = ComputeStatusForGoal(goal, today)
        };
    }
}
