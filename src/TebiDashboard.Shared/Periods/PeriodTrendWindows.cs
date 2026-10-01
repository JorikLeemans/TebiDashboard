using System.Globalization;
using TebiDashboard.Shared.Formatting;

namespace TebiDashboard.Shared.Periods;

/// <summary>
/// One inclusive date window used when comparing revenue across periods of equal length.
/// </summary>
public readonly record struct PeriodTrendWindow(DateOnly From, DateOnly To);

/// <summary>
/// Builds the selected period and the equal-length periods immediately before it.
/// </summary>
public static class PeriodTrendWindows
{
    /// <summary>Selected period plus the two windows before it.</summary>
    public const int DefaultCount = 3;

    /// <summary>
    /// Oldest window first. The last window is the selected period.
    /// </summary>
    public static IReadOnlyList<PeriodTrendWindow> Build(DateOnly from, DateOnly to, int count = DefaultCount)
    {
        if (to < from)
        {
            (from, to) = (to, from);
        }

        if (count < 1)
        {
            count = 1;
        }

        var length = to.DayNumber - from.DayNumber + 1;
        var windows = new List<PeriodTrendWindow>(count);
        for (var stepsBack = count - 1; stepsBack >= 0; stepsBack--)
        {
            var windowTo = to.AddDays(-stepsBack * length);
            var windowFrom = windowTo.AddDays(-(length - 1));
            windows.Add(new PeriodTrendWindow(windowFrom, windowTo));
        }

        return windows;
    }

    /// <summary>
    /// Short change versus the previous window. A missing previous window has no comparison.
    /// </summary>
    public static string DescribeChange(decimal current, decimal? previous)
    {
        if (previous is null)
        {
            return "—";
        }

        var delta = current - previous.Value;
        if (delta == 0)
        {
            return "0%";
        }

        // A zero baseline has no percentage. Show the euro difference instead.
        if (previous.Value == 0)
        {
            return EuroFormatting.FormatWhole(delta);
        }

        var percent = Math.Round(delta / previous.Value * 100m, 0, MidpointRounding.AwayFromZero);
        var sign = percent > 0 ? "+" : "";
        return sign + percent.ToString("0", CultureInfo.GetCultureInfo("nl-NL")) + "%";
    }

    /// <summary>Word used on the revenue card: up, down, or flat versus the previous window.</summary>
    public static string TrendWord(decimal current, decimal previous)
    {
        if (current == previous)
        {
            return "flat";
        }

        return current > previous ? "up" : "down";
    }
}
