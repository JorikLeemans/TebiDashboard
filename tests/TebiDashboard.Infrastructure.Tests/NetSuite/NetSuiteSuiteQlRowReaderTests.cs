using System.Text.Json;
using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Unit tests for SuiteQL JSON row parsing helpers.
/// </summary>
public sealed class NetSuiteSuiteQlRowReaderTests
{
    [Fact]
    public void GetDecimal_ParsesNumberAndStringValues()
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["amount"] = JsonDocument.Parse("123.45").RootElement,
            ["quantity"] = JsonDocument.Parse("\"9.5\"").RootElement
        };

        Assert.Equal(123.45m, NetSuiteSuiteQlRowReader.GetDecimal(row, "amount"));
        Assert.Equal(9.5m, NetSuiteSuiteQlRowReader.GetDecimal(row, "quantity"));
    }

    [Fact]
    public void GetDate_ParsesIsoDateStrings()
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["trandate"] = JsonDocument.Parse("\"2025-06-01\"").RootElement
        };

        Assert.Equal(new DateOnly(2025, 6, 1), NetSuiteSuiteQlRowReader.GetDate(row, "trandate"));
    }

    [Theory]
    [InlineData("2025-06-01T00:00:00Z", 2025, 6, 1)]
    [InlineData("8/20/2026", 2026, 8, 20)]
    [InlineData("1/15/2026", 2026, 1, 15)]
    [InlineData("24/09/2025", 2025, 9, 24)]
    [InlineData("24-9-2025", 2025, 9, 24)]
    [InlineData("24.09.2025", 2025, 9, 24)]
    [InlineData("24/09/2025 0:00:00", 2025, 9, 24)]
    [InlineData("24-9-2025 0:00:00", 2025, 9, 24)]
    [InlineData("01-06-2025", 2025, 6, 1)]
    [InlineData("15-JAN-2024", 2024, 1, 15)]
    public void GetDate_ParsesNetSuiteDateFormats(string value, int year, int month, int day)
    {
        var row = Row("trandate", value);

        var parsed = NetSuiteSuiteQlRowReader.GetDate(row, "tranDate");

        Assert.Equal(new DateOnly(year, month, day), parsed);
    }

    [Fact]
    public void GetDate_ReadsQualifiedTranDateColumn()
    {
        // SuiteQL can return the select expression instead of a bare alias.
        var row = Row("so.tranDate", "24/09/2025");

        Assert.Equal(new DateOnly(2025, 9, 24), NetSuiteSuiteQlRowReader.GetDate(row, "trandate", "tranDate"));
    }

    [Fact]
    public void GetDate_TreatsAmbiguousSlashDatesAsSuiteQlMonthFirst()
    {
        // Observed SuiteQL serialization is M/D/YYYY, so 01/06/2025 is 6 January.
        // Day-first dash dates are covered separately (01-06-2025 = 1 June).
        var row = Row("trandate", "01/06/2025");

        Assert.Equal(new DateOnly(2025, 1, 6), NetSuiteSuiteQlRowReader.GetDate(row, "trandate"));
    }

    [Fact]
    public void TryGetDate_DoesNotSubstituteTodayWhenValueIsPresentButUnparseable()
    {
        var row = Row("trandate", "not-a-date");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var parsed = NetSuiteSuiteQlRowReader.TryGetDate(row, out var date, "trandate");

        Assert.False(parsed);
        Assert.NotEqual(today, date);
        var exception = Assert.Throws<InvalidOperationException>(() => NetSuiteSuiteQlRowReader.GetDate(row, "trandate"));
        Assert.Contains("not-a-date", exception.Message);
    }

    [Fact]
    public void TryGetDate_ReturnsFalseWhenDateColumnIsMissing()
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        Assert.False(NetSuiteSuiteQlRowReader.TryGetDate(row, out _, "trandate", "tranDate"));
    }

    private static Dictionary<string, JsonElement> Row(string key, string value) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [key] = JsonDocument.Parse($"\"{value}\"").RootElement
        };
}
