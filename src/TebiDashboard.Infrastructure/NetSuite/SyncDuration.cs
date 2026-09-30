namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Wall-clock duration appended to the admin sync success message.
/// </summary>
internal static class SyncDuration
{
    /// <summary>
    /// Adds "Completed in …" when the run has a start time. A missing start leaves the message unchanged.
    /// </summary>
    public static string Append(string message, DateTimeOffset? startedAt, DateTimeOffset completedAt)
    {
        if (startedAt is null)
        {
            return message;
        }

        var elapsed = completedAt - startedAt.Value;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        var duration = Format(elapsed);
        if (string.IsNullOrWhiteSpace(message))
        {
            return $"Completed in {duration}.";
        }

        return $"{message.TrimEnd()} Completed in {duration}.";
    }

    /// <summary>Compact elapsed text: 12s, 2m 05s, or 1h 03m 09s.</summary>
    public static string Format(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
        {
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes:00}m {elapsed.Seconds:00}s";
        }

        if (elapsed.TotalMinutes >= 1)
        {
            return $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds:00}s";
        }

        return $"{Math.Max(0, (int)elapsed.TotalSeconds)}s";
    }
}
