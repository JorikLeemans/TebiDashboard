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

    [Fact]
    public void GetQuarterDateRange_ReturnsExpectedBounds()
    {
        var referenceDate = new DateOnly(2026, 6, 15);
        var (from, to) = PeriodHelper.GetQuarterDateRange(2026, "Q2", referenceDate);

        Assert.Equal(new DateOnly(2026, 4, 1), from);
        Assert.Equal(referenceDate, to);
    }

    [Theory]
    [InlineData(2025, "Q4", 2026, 6, 15, true)]
    [InlineData(2026, "Q1", 2026, 6, 15, true)]
    [InlineData(2026, "Q2", 2026, 6, 15, false)]
    [InlineData(2026, "Q3", 2026, 6, 15, false)]
    [InlineData(2027, "Q1", 2026, 6, 15, false)]
    public void IsPeriodCompleted_ReturnsExpectedResult(
        int goalYear,
        string goalPeriod,
        int referenceYear,
        int referenceMonth,
        int referenceDay,
        bool expected)
    {
        var referenceDate = new DateOnly(referenceYear, referenceMonth, referenceDay);
        var completed = PeriodHelper.IsPeriodCompleted(goalYear, goalPeriod, referenceDate);

        Assert.Equal(expected, completed);
    }
}
