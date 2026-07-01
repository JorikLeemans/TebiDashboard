using System.Globalization;
using System.Text.Json;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Helpers for reading loosely typed SuiteQL JSON rows.
/// </summary>
internal static class NetSuiteSuiteQlRowReader
{
    public static string? GetString(IReadOnlyDictionary<string, JsonElement> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
            else if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return value.ToString();
            }
        }

        return null;
    }

    public static string GetRequiredString(IReadOnlyDictionary<string, JsonElement> row, params string[] keys) =>
        GetString(row, keys) ?? throw new InvalidOperationException($"SuiteQL row is missing required field: {keys[0]}.");

    public static decimal GetDecimal(IReadOnlyDictionary<string, JsonElement> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var value))
            {
                continue;
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.Number when value.TryGetDecimal(out var number):
                    return number;
                case JsonValueKind.String when decimal.TryParse(
                    value.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsed):
                    return parsed;
            }
        }

        return 0m;
    }

    public static DateOnly GetDate(IReadOnlyDictionary<string, JsonElement> row, params string[] keys)
    {
        var text = GetString(row, keys);
        if (string.IsNullOrWhiteSpace(text))
        {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }

        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
        {
            return isoDate;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
