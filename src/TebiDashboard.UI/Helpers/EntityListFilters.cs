namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Client-side text matching for dashboard list pages.
/// </summary>
public static class EntityListFilters
{
    public static bool MatchesSearch(string? query, params string?[] fields)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var trimmed = query.Trim();
        foreach (var field in fields)
        {
            if (!string.IsNullOrWhiteSpace(field)
                && field.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
