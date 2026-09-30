using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

/// <summary>
/// Sync times are stored in UTC and shown in Netherlands local time.
/// </summary>
public sealed class AmsterdamTimeTests
{
    [Fact]
    public void Format_AddsTwoHours_DuringCentralEuropeanSummerTime()
    {
        var utc = DateTimeOffset.Parse("2026-09-30T06:45:00Z");

        var amsterdam = TimeZoneInfo.ConvertTime(utc, AmsterdamTime.Zone);

        Assert.Equal(8, amsterdam.Hour);
        Assert.Equal(45, amsterdam.Minute);
        Assert.Equal(amsterdam.ToString("g"), AmsterdamTime.Format(utc));
    }
}
