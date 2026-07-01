using System.Text.Json;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Removes duplicate SuiteQL rows that can appear when NetSuite pages overlap.
/// </summary>
internal static class NetSuiteSuiteQlRowDeduplicator
{
    public static IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> ByField(
        IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> rows,
        string keyField)
    {
        var uniqueRows = new Dictionary<string, IReadOnlyDictionary<string, JsonElement>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var key = NetSuiteSuiteQlRowReader.GetString(row, keyField);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            // Last row wins so later pages can refresh the same master record.
            uniqueRows[key] = row;
        }

        return uniqueRows.Values.ToList();
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> ByFields(
        IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> rows,
        params string[] keyFields)
    {
        var uniqueRows = new Dictionary<string, IReadOnlyDictionary<string, JsonElement>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var parts = keyFields
                .Select(field => NetSuiteSuiteQlRowReader.GetString(row, field) ?? string.Empty)
                .ToArray();

            if (parts.Any(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var key = string.Join('|', parts);
            uniqueRows[key] = row;
        }

        return uniqueRows.Values.ToList();
    }
}
