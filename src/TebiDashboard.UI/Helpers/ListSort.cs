namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Applies <see cref="ListSortState"/> to an in-memory list. Strings compare
/// case-insensitively; null keys sort last when ascending.
/// </summary>
public static class ListSort
{
    public static List<T> Apply<T>(
        IEnumerable<T> items,
        ListSortState sort,
        Func<T, object?> keySelector)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(sort);
        ArgumentNullException.ThrowIfNull(keySelector);

        if (!sort.IsActive)
        {
            return items as List<T> ?? items.ToList();
        }

        return sort.Ascending
            ? items.OrderBy(keySelector, ListSortComparer.Instance).ToList()
            : items.OrderByDescending(keySelector, ListSortComparer.Instance).ToList();
    }

    /// <summary>
    /// Sorts the full list when a column is active, then keeps the dashboard preview cap.
    /// Unsorted tables keep their original (usually newest-first) order.
    /// </summary>
    public static IReadOnlyList<T> ApplyThenPreview<T>(
        IReadOnlyList<T> items,
        ListSortState sort,
        Func<T, object?> keySelector)
    {
        var sorted = Apply(items, sort, keySelector);
        return DashboardHomePreview.MostRecent(sorted);
    }
}
