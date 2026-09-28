namespace TebiDashboard.Shared.Constants;

/// <summary>
/// Shared lookback bounds for Admin slider and API clamp. SuiteQL invoice/bill queries
/// filter on tranDate &gt;= today minus these days (see NetSuite SuiteQL REST paging docs).
/// </summary>
public static class NetSuiteLookback
{
    public const int MinDays = 1;

    /// <summary>Two years is enough for an occasional backfill without a huge unbounded query.</summary>
    public const int MaxDays = 730;

    /// <summary>Typical refresh after data is already stored.</summary>
    public const int DefaultDays = 30;

    /// <summary>First import covers a full calendar year of invoices.</summary>
    public const int FirstImportDays = 365;

    /// <summary>Extra days on top of "time since last success" so late-posted invoices are included.</summary>
    public const int RefreshBufferDays = 7;

    public static int Clamp(int days) => Math.Clamp(days, MinDays, MaxDays);

    /// <summary>
    /// Suggests a lookback: a full year when nothing is stored yet, otherwise days since last
    /// successful sync plus a small buffer (minimum <see cref="DefaultDays"/>).
    /// </summary>
    public static int Suggest(DateTimeOffset? lastSucceededAt, DateTimeOffset utcNow)
    {
        if (lastSucceededAt is null)
        {
            return FirstImportDays;
        }

        var elapsedDays = (int)Math.Ceiling(Math.Max(0, (utcNow - lastSucceededAt.Value).TotalDays));
        return Clamp(Math.Max(elapsedDays + RefreshBufferDays, DefaultDays));
    }
}
