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

    // Regression test for the "revenue dates show today" bug: NetSuite renders dates using the
    // account locale, so a Dutch (TEBI) account returns day-first values. When the day is > 12 the
    // old invariant (US) parser failed and silently returned DateOnly.FromDateTime(DateTime.UtcNow).
    // These cases prove the real transaction date is preserved instead.
    [Theory]
    [InlineData("25/06/2025", 2025, 6, 25)]
    [InlineData("31/12/2025", 2025, 12, 31)]
    [InlineData("13/06/2025", 2025, 6, 13)]
    [InlineData("25-06-2025", 2025, 6, 25)]
    [InlineData("25/06/2025 00:00:00", 2025, 6, 25)]
    public void GetDate_ParsesDutchDayFirstDates(string value, int year, int month, int day)
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["trandate"] = JsonDocument.Parse($"\"{value}\"").RootElement
        };

        Assert.Equal(new DateOnly(year, month, day), NetSuiteSuiteQlRowReader.GetDate(row, "trandate"));
    }

    [Fact]
    public void GetDate_TreatsAmbiguousSlashDatesAsDayFirst()
    {
        // "01/06/2025" must be read as 1 June (NL) rather than 6 January (US) so day-first accounts
        // are not silently corrupted.
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["trandate"] = JsonDocument.Parse("\"01/06/2025\"").RootElement
        };

        Assert.Equal(new DateOnly(2025, 6, 1), NetSuiteSuiteQlRowReader.GetDate(row, "trandate"));
    }

    [Fact]
    public void GetDate_ThrowsInsteadOfDefaultingToTodayOnUnparseableValue()
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["trandate"] = JsonDocument.Parse("\"not-a-date\"").RootElement
        };

        var exception = Assert.Throws<InvalidOperationException>(() => NetSuiteSuiteQlRowReader.GetDate(row, "trandate"));
        Assert.Contains("unparseable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryGetDate_ReturnsFalseForMissingOrUnparseableValues()
    {
        var missing = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        Assert.False(NetSuiteSuiteQlRowReader.TryGetDate(missing, out _, "trandate"));

        var unparseable = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["trandate"] = JsonDocument.Parse("\"garbage\"").RootElement
        };
        Assert.False(NetSuiteSuiteQlRowReader.TryGetDate(unparseable, out _, "trandate"));
    }

    [Fact]
    public void TryGetDate_NeverReturnsCurrentDateForBadValue()
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["trandate"] = JsonDocument.Parse("\"99/99/9999\"").RootElement
        };

        var parsed = NetSuiteSuiteQlRowReader.TryGetDate(row, out var date, "trandate");

        Assert.False(parsed);
        Assert.NotEqual(DateOnly.FromDateTime(DateTime.UtcNow), date);
    }
}
