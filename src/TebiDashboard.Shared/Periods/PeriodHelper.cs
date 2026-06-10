namespace TebiDashboard.Shared.Periods;

/// <summary>
/// Helpers for mapping calendar dates to business periods such as quarters.
/// </summary>
public static class PeriodHelper
{
    /// <summary>
    /// Returns the quarter label (Q1–Q4) for a given date.
    /// </summary>
    public static string GetQuarter(DateOnly date) => date.Month switch
    {
        <= 3 => "Q1",
        <= 6 => "Q2",
        <= 9 => "Q3",
        _ => "Q4"
    };
}
