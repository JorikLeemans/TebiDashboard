using TebiDashboard.Shared.Periods;

namespace TebiDashboard.Application.Tests;

public class PeriodHelperTests
{
    [Theory]
    [InlineData(2026, 1, "Q1")]
    [InlineData(2026, 6, "Q2")]
    [InlineData(2026, 9, "Q3")]
    [InlineData(2026, 12, "Q4")]
    public void GetQuarter_ReturnsExpectedLabel(int year, int month, string expected)
    {
        var quarter = PeriodHelper.GetQuarter(new DateOnly(year, month, 15));

        Assert.Equal(expected, quarter);
    }

    [Theory]
    [InlineData("Q1", 1)]
    [InlineData("Q4", 4)]
    [InlineData("Unknown", 0)]
    public void GetQuarterIndex_ReturnsExpectedValue(string period, int expected)
    {
        Assert.Equal(expected, PeriodHelper.GetQuarterIndex(period));
    }
}
