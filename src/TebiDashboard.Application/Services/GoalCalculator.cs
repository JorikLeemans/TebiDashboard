using TebiDashboard.Shared.Dtos;

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
    /// Enriches a goal DTO with calculated percentage and status.
    /// </summary>
    public static GoalDto WithProgress(GoalDto goal)
    {
        var percentage = ComputePercentage(goal.ActualValue, goal.TargetValue);
        return goal with
        {
            Percentage = percentage,
            Status = ComputeStatus(percentage)
        };
    }
}
