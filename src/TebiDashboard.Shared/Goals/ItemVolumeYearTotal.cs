namespace TebiDashboard.Shared.Goals;

/// <summary>
/// Year total for one item volume goal.
/// Quarters that have a value replace a typed year total with their sum.
/// A year total is kept when every quarter is empty.
/// </summary>
public static class ItemVolumeYearTotal
{
    /// <summary>
    /// Returns the quarter sum when any quarter is set, otherwise the existing year total.
    /// Zero and empty quarters do not count as a value.
    /// </summary>
    public static decimal? Resolve(IReadOnlyDictionary<string, decimal?> targets)
    {
        if (HasQuarterValue(targets))
        {
            return SumQuarters(targets);
        }

        return targets.TryGetValue(GoalPeriods.Year, out var yearTotal) ? yearTotal : null;
    }

    /// <summary>
    /// True when at least one quarter target is greater than zero.
    /// </summary>
    public static bool HasQuarterValue(IReadOnlyDictionary<string, decimal?> targets) =>
        QuarterPeriods.Any(period => ReadPositive(targets, period) is not null);

    private static decimal SumQuarters(IReadOnlyDictionary<string, decimal?> targets) =>
        QuarterPeriods.Sum(period => ReadPositive(targets, period) ?? 0m);

    private static decimal? ReadPositive(IReadOnlyDictionary<string, decimal?> targets, string period)
    {
        if (targets.TryGetValue(period, out var value) && value is > 0m)
        {
            return value;
        }

        return null;
    }

    private static readonly string[] QuarterPeriods =
    [
        GoalPeriods.Q1,
        GoalPeriods.Q2,
        GoalPeriods.Q3,
        GoalPeriods.Q4
    ];
}
