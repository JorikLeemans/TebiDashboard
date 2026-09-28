namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Page size options and helpers for dashboard list pagination.
/// </summary>
public static class ListPagination
{
    public static readonly int[] AllowedPageSizes = [5, 10, 20, 25, 50, 100];

    public const int DefaultPageSize = 25;

    public static int GetTotalPages(int totalItems, int pageSize) =>
        totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);

    public static int ClampPage(int page, int totalPages) =>
        Math.Clamp(page, 1, Math.Max(1, totalPages));

    public static List<T> GetPageSlice<T>(IReadOnlyList<T> items, int page, int pageSize)
    {
        if (items.Count == 0)
        {
            return [];
        }

        return items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public static (int Start, int End) GetVisibleRange(int page, int pageSize, int totalItems)
    {
        if (totalItems == 0)
        {
            return (0, 0);
        }

        var start = ((page - 1) * pageSize) + 1;
        var end = Math.Min(page * pageSize, totalItems);
        return (start, end);
    }
}
