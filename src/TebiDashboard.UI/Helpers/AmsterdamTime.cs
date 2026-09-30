namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Formats sync timestamps in Netherlands time. The API stores UTC, and the host
/// often runs in UTC, so <see cref="DateTimeOffset.LocalDateTime"/> is two hours early
/// during Central European Summer Time.
/// </summary>
public static class AmsterdamTime
{
    /// <summary>Europe/Amsterdam, with the Windows id as a fallback.</summary>
    public static TimeZoneInfo Zone { get; } = ResolveZone();

    /// <summary>Short local date and time in Amsterdam, for example 30-9-2026 08:45.</summary>
    public static string Format(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Zone).ToString("g");

    private static TimeZoneInfo ResolveZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Amsterdam", out var iana))
        {
            return iana;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById("W. Europe Standard Time", out var windows))
        {
            return windows;
        }

        return TimeZoneInfo.Local;
    }
}
