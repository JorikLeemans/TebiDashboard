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

    /// <summary>
    /// Maps a quarter label to a sortable index (Q1 = 1, unknown = 0).
    /// </summary>
    public static int GetQuarterIndex(string period) => period switch
    {
        "Q1" => 1,
        "Q2" => 2,
        "Q3" => 3,
        "Q4" => 4,
        _ => 0
    };
}
