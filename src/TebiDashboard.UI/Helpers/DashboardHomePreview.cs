namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Caps the home dashboard tables so a large NetSuite import does not fill the whole screen.
/// KPIs and the revenue chart still use the full period list.
/// </summary>
public static class DashboardHomePreview
{
    /// <summary>Maximum rows shown in each home-dashboard table.</summary>
    public const int MaxRows = 20;

    /// <summary>
    /// Returns the first MaxRows items. Callers should pass lists already sorted newest-first.
    /// </summary>
    public static IReadOnlyList<T> MostRecent<T>(IReadOnlyList<T> items)
    {
        if (items.Count <= MaxRows)
        {
            return items;
        }

        return items.Take(MaxRows).ToList();
    }

    /// <summary>Heading count: "12" or "20 of 1842".</summary>
    public static string TitleCount(int shown, int total) =>
        total <= MaxRows ? total.ToString() : $"{shown} of {total}";

    /// <summary>Note under a truncated table; empty when everything fits.</summary>
    public static string TruncationNote(int total, bool mostRecent, bool includeChart = true)
    {
        if (total <= MaxRows)
        {
            return string.Empty;
        }

        if (!mostRecent)
        {
            return $"Showing {MaxRows} of {total}.";
        }

        var totalsHint = includeChart
            ? "Period totals stay in the KPIs and chart."
            : "Period totals stay in the KPIs.";
        return $"Showing the {MaxRows} most recent of {total}. {totalsHint}";
    }
}
