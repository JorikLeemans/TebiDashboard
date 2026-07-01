using System.Text.Json;
using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Unit tests for SuiteQL row deduplication during sync.
/// </summary>
public sealed class NetSuiteSuiteQlRowDeduplicatorTests
{
    [Fact]
    public void ByField_KeepsSingleRowPerId()
    {
        var rows = new List<IReadOnlyDictionary<string, JsonElement>>
        {
            Row("id", "10", "companyname", "Acme"),
            Row("id", "10", "companyname", "Acme Updated"),
            Row("id", "11", "companyname", "Beta")
        };

        var deduped = NetSuiteSuiteQlRowDeduplicator.ByField(rows, "id");

        Assert.Equal(2, deduped.Count);
        Assert.Equal("Acme Updated", NetSuiteSuiteQlRowReader.GetString(deduped.Single(r => NetSuiteSuiteQlRowReader.GetString(r, "id") == "10"), "companyname"));
    }

    private static Dictionary<string, JsonElement> Row(params string[] keyValues)
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < keyValues.Length; i += 2)
        {
            row[keyValues[i]] = JsonDocument.Parse($"\"{keyValues[i + 1]}\"").RootElement.Clone();
        }

        return row;
    }
}
