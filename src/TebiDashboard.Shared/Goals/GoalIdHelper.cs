namespace TebiDashboard.Shared.Goals;

/// <summary>
/// Builds stable goal identifiers used by the demo seeder and management API.
/// </summary>
public static class GoalIdHelper
{
    /// <summary>
    /// Builds the primary key for a company revenue goal.
    /// </summary>
    public static string BuildCompanyGoalId(int year, string period) =>
        $"g-{year}-{period.ToLowerInvariant()}";

    /// <summary>
    /// Builds the primary key for an item volume goal.
    /// </summary>
    public static string BuildItemGoalId(int year, string itemId, string period) =>
        $"g-{year}-{itemId}-{period.ToLowerInvariant()}";

    /// <summary>
    /// Returns a user-facing label for a goal period.
    /// </summary>
    public static string FormatPeriodLabel(string period) =>
        string.Equals(period, GoalPeriods.Year, StringComparison.OrdinalIgnoreCase)
            ? "Year total"
            : period;
}
