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
}
