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

    /// <summary>
    /// Returns true when the goal period has fully ended (prior years or earlier quarters).
    /// </summary>
    public static bool IsPeriodCompleted(int year, string period, DateOnly referenceDate)
    {
        var periodIndex = GetQuarterIndex(period);
        if (periodIndex == 0)
        {
            return false;
        }

        if (year < referenceDate.Year)
        {
            return true;
        }

        if (year > referenceDate.Year)
        {
            return false;
        }

        var currentQuarterIndex = GetQuarterIndex(GetQuarter(referenceDate));
        return periodIndex < currentQuarterIndex;
    }

    /// <summary>
    /// Returns the inclusive calendar range for a quarter, capped at the reference date for the current year.
    /// </summary>
    public static (DateOnly From, DateOnly To) GetQuarterDateRange(int year, string period, DateOnly referenceDate)
    {
        var periodIndex = GetQuarterIndex(period);
        if (periodIndex == 0)
        {
            var yearStart = new DateOnly(year, 1, 1);
            var yearEnd = new DateOnly(year, 12, 31);
            if (year == referenceDate.Year)
            {
                yearEnd = referenceDate;
            }

            return (yearStart, yearEnd);
        }

        var fromMonth = periodIndex switch
        {
            1 => 1,
            2 => 4,
            3 => 7,
            _ => 10
        };

        var from = new DateOnly(year, fromMonth, 1);
        var to = periodIndex switch
        {
            1 => new DateOnly(year, 3, 31),
            2 => new DateOnly(year, 6, 30),
            3 => new DateOnly(year, 9, 30),
            _ => new DateOnly(year, 12, 31)
        };

        if (year == referenceDate.Year && to > referenceDate)
        {
            to = referenceDate;
        }

        return (from, to);
    }
}
