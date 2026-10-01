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

            var text = ElementToString(value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
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

    /// <summary>
    /// True when one of the keys is present as a number or numeric string, including zero.
    /// A missing column is not the same as a stored zero.
    /// </summary>
    public static bool HasDecimal(IReadOnlyDictionary<string, JsonElement> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var value))
            {
                continue;
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.Number:
                    return true;
                case JsonValueKind.String when decimal.TryParse(
                    value.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out _):
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads a transaction date. Returns <c>false</c> when the value is absent or cannot be parsed.
    /// A present-but-unparseable value is a failure: callers must not substitute UTC today, which
    /// previously made every invoice and open order look like it happened today.
    /// </summary>
    public static bool TryGetDate(IReadOnlyDictionary<string, JsonElement> row, out DateOnly date, params string[] keys)
    {
        date = default;
        var text = FindDateText(row, keys);
        return text is not null && TryParseTransactionDate(text, out date);
    }

    /// <summary>
    /// Reads a required transaction date. Throws when the value is missing or unparseable so the
    /// failure stays visible instead of being stored as the current day.
    /// </summary>
    public static DateOnly GetDate(IReadOnlyDictionary<string, JsonElement> row, params string[] keys)
    {
        if (TryGetDate(row, out var date, keys))
        {
            return date;
        }

        var raw = FindDateText(row, keys);
        throw new InvalidOperationException(
            raw is null
                ? $"SuiteQL row is missing required date field: {keys[0]}."
                : $"SuiteQL row has an unparseable date for field '{keys[0]}': '{raw}'.");
    }

    /// <summary>
    /// SuiteQL REST lowercases aliases, and a qualified select such as <c>so.tranDate</c> can come
    /// back as <c>trandate</c> or as <c>so.trandate</c>. Match the requested name and a qualified suffix.
    /// </summary>
    private static string? FindDateText(IReadOnlyDictionary<string, JsonElement> row, string[] keys)
    {
        var direct = GetString(row, keys);
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct.Trim().Trim('"');
        }

        foreach (var key in keys)
        {
            foreach (var pair in row)
            {
                if (!pair.Key.EndsWith("." + key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = ElementToString(pair.Value);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim().Trim('"');
                }
            }
        }

        return null;
    }

    private static string? ElementToString(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
        {
            return value.ToString();
        }

        return null;
    }

    /// <summary>
    /// Parses the date strings SuiteQL actually returns.
    /// Unformatted columns follow the account date preference: observed REST samples are
    /// <c>M/D/YYYY</c> (for example <c>8/20/2026</c> and <c>1/15/2026</c>). A Dutch preference
    /// returns day-first values such as <c>24/09/2025</c> or <c>24-9-2025</c>. Invariant (US)
    /// parsing rejects any day greater than 12 in those shapes, which is what collapsed the
    /// transaction date to UTC today. Queries also project <c>TO_CHAR(..., 'YYYY-MM-DD')</c>,
    /// so the primary path is an unambiguous ISO date. The calendar date is taken from the
    /// text itself; a trailing time or offset must not roll the day.
    /// </summary>
    internal static bool TryParseTransactionDate(string text, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();

        // ISO-8601 date, optionally followed by a time ("2025-06-01" or "2025-06-01T00:00:00Z").
        if (trimmed.Length >= 10
            && trimmed[4] == '-'
            && trimmed[7] == '-'
            && DateOnly.TryParseExact(
                trimmed.AsSpan(0, 10),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            return true;
        }

        var datePart = trimmed;
        var space = trimmed.IndexOf(' ');
        if (space > 0)
        {
            datePart = trimmed[..space];
        }

        var parts = datePart.Split(['/', '-', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 3
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var first)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var second)
            && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var year))
        {
            if (parts[0].Length == 4)
            {
                // yyyy-M-d. Do not treat the day as a two-digit year.
                if (!IsValidDate(first, second, year))
                {
                    return false;
                }

                date = new DateOnly(first, second, year);
                return true;
            }

            if (year is >= 0 and < 100)
            {
                year += 2000;
            }

            var monthFirst = IsValidDate(year, first, second) ? new DateOnly(year, first, second) : (DateOnly?)null;
            var dayFirst = IsValidDate(year, second, first) ? new DateOnly(year, second, first) : (DateOnly?)null;

            if (monthFirst is null && dayFirst is null)
            {
                return false;
            }

            // Only one reading is a real calendar date (day > 12). Use that one.
            if (monthFirst is null)
            {
                date = dayFirst!.Value;
                return true;
            }

            if (dayFirst is null)
            {
                date = monthFirst.Value;
                return true;
            }

            // Both readings are valid (both components <= 12).
            // '/' matches the SuiteQL M/D/YYYY serialization. '-' and '.' are the European
            // day-first forms used by a Dutch NetSuite date preference (01-06-2025 = 1 June).
            var europeanSeparator = datePart.Contains('-') || datePart.Contains('.');
            date = europeanSeparator ? dayFirst.Value : monthFirst.Value;
            return true;
        }

        // Oracle-style month names ("15-JAN-2024") and other shapes the invariant parser accepts.
        // This runs after the numeric rules so a day-first dash date is not re-read as US month-first.
        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var named))
        {
            date = DateOnly.FromDateTime(named);
            return true;
        }

        return false;
    }

    private static bool IsValidDate(int year, int month, int day) =>
        month is >= 1 and <= 12
        && day is >= 1 and <= 31
        && year is >= 1 and <= 9999
        && day <= DateTime.DaysInMonth(year, month);
}
