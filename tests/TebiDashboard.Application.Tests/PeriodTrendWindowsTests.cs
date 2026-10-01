using TebiDashboard.Shared.Periods;

namespace TebiDashboard.Application.Tests;

public class PeriodTrendWindowsTests
{
    [Fact]
    public void Build_ThreeDayWindow_IncludesTheTwoPeriodsBeforeIt()
    {
        var windows = PeriodTrendWindows.Build(new DateOnly(2026, 9, 29), new DateOnly(2026, 10, 1));

        Assert.Equal(3, windows.Count);
        Assert.Equal(new DateOnly(2026, 9, 23), windows[0].From);
        Assert.Equal(new DateOnly(2026, 9, 25), windows[0].To);
        Assert.Equal(new DateOnly(2026, 9, 26), windows[1].From);
        Assert.Equal(new DateOnly(2026, 9, 28), windows[1].To);
        Assert.Equal(new DateOnly(2026, 9, 29), windows[2].From);
        Assert.Equal(new DateOnly(2026, 10, 1), windows[2].To);
    }

    [Fact]
    public void DescribeChange_UsesPercentAgainstThePreviousWindow()
    {
        Assert.Equal("—", PeriodTrendWindows.DescribeChange(100m, null));
        Assert.Equal("0%", PeriodTrendWindows.DescribeChange(100m, 100m));
        Assert.Equal("+18%", PeriodTrendWindows.DescribeChange(118m, 100m));
        Assert.Equal("-25%", PeriodTrendWindows.DescribeChange(75m, 100m));
    }

    [Fact]
    public void TrendWord_MatchesTheDirectionOfTheChange()
    {
        Assert.Equal("up", PeriodTrendWindows.TrendWord(200m, 100m));
        Assert.Equal("down", PeriodTrendWindows.TrendWord(50m, 100m));
        Assert.Equal("flat", PeriodTrendWindows.TrendWord(100m, 100m));
    }
}
