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

    /// <summary>
    /// Explicit date formats we accept from SuiteQL, ordered by priority.
    /// NetSuite renders date columns using the account/user locale preference, so a Dutch
    /// (TEBI) account returns day-first values such as "25/06/2025". The invariant (US, MM/dd)
    /// parser rejects any day &gt; 12, which previously fell through to "today" and masked the
    /// real transaction date. ISO is tried first (unambiguous), then day-first European
    /// variants, then Oracle-style month abbreviations. Because day-first is listed before the
    /// lenient invariant fallback, ambiguous values like "01/06/2025" are read as 1 June (the
    /// NL interpretation) instead of being mis-parsed as 6 January.
    /// </summary>
    private static readonly string[] DateFormats =
    [
        // Unambiguous ISO output (SuiteQL TO_CHAR / default serialisation).
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        // Day-first (European / Dutch locale) date and datetime variants.
        "d/M/yyyy",
        "dd/MM/yyyy",
        "d-M-yyyy",
        "dd-MM-yyyy",
        "d/M/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "d/M/yyyy h:mm:ss tt",
        "dd/MM/yyyy h:mm:ss tt",
        // Oracle-style month abbreviation seen in some SuiteQL configurations.
        "d-MMM-yyyy",
        "dd-MMM-yyyy"
    ];

    /// <summary>
    /// Attempts to read a transaction date from the row. Returns <c>false</c> when the value is
    /// absent or cannot be parsed. Crucially, this never substitutes the current date, so callers
    /// can detect and skip/log bad rows instead of silently persisting "today".
    /// </summary>
    public static bool TryGetDate(IReadOnlyDictionary<string, JsonElement> row, out DateOnly date, params string[] keys)
    {
        date = default;

        var text = GetString(row, keys)?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Explicit formats first so day-first NL dates (including day > 12) parse correctly.
        if (DateTime.TryParseExact(
                text,
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces,
                out var exact))
        {
            date = DateOnly.FromDateTime(exact);
            return true;
        }

        // Lenient invariant parse as a last resort (covers US M/d/yyyy and other odd shapes).
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
        {
            date = DateOnly.FromDateTime(dateTime);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads a required transaction date. Throws when the value is missing or unparseable instead
    /// of masking the problem as the current date (the original bug), so failures surface loudly.
    /// </summary>
    public static DateOnly GetDate(IReadOnlyDictionary<string, JsonElement> row, params string[] keys)
    {
        if (TryGetDate(row, out var date, keys))
        {
            return date;
        }

        var raw = GetString(row, keys);
        throw new InvalidOperationException(
            raw is null
                ? $"SuiteQL row is missing required date field: {keys[0]}."
                : $"SuiteQL row has an unparseable date for field '{keys[0]}': '{raw}'.");
    }
}
