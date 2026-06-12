namespace TebiDashboard.Shared.Goals;

/// <summary>
/// Supported goal period labels for company and item targets.
/// </summary>
public static class GoalPeriods
{
    public const string Q1 = "Q1";
    public const string Q2 = "Q2";
    public const string Q3 = "Q3";
    public const string Q4 = "Q4";

    /// <summary>
    /// Full-calendar-year revenue or volume target.
    /// </summary>
    public const string Year = "YEAR";

    /// <summary>
    /// Company-level periods shown on the goal settings page.
    /// </summary>
    public static IReadOnlyList<string> CompanyPeriods { get; } = [Q1, Q2, Q3, Q4, Year];

    /// <summary>
    /// Item-level periods shown on the goal settings page.
    /// </summary>
    public static IReadOnlyList<string> ItemPeriods { get; } = [Q1, Q2, Q3, Q4, Year];

    public static bool IsQuarter(string period) =>
        period is Q1 or Q2 or Q3 or Q4;

    public static bool IsSupported(string period) =>
        IsQuarter(period) || string.Equals(period, Year, StringComparison.OrdinalIgnoreCase);
}
