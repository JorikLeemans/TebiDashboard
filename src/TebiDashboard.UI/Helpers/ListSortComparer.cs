namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Shared comparer for mixed list-sort keys (text, numbers, dates, bools).
/// </summary>
internal sealed class ListSortComparer : IComparer<object?>
{
    public static readonly ListSortComparer Instance = new();

    public int Compare(object? x, object? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return 1;
        }

        if (y is null)
        {
            return -1;
        }

        if (x is string leftText && y is string rightText)
        {
            return string.Compare(leftText, rightText, StringComparison.OrdinalIgnoreCase);
        }

        if (x is IComparable left && x.GetType() == y.GetType())
        {
            return left.CompareTo(y);
        }

        return string.Compare(
            Convert.ToString(x),
            Convert.ToString(y),
            StringComparison.OrdinalIgnoreCase);
    }
}
