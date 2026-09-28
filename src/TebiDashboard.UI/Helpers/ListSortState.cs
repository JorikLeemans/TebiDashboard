namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Click-to-sort state for one table. The first click on a column sorts ascending;
/// a second click on the same column reverses the direction.
/// </summary>
public sealed class ListSortState
{
    public string? Column { get; private set; }

    public bool Ascending { get; private set; } = true;

    /// <summary>True once the user has chosen a column.</summary>
    public bool IsActive => !string.IsNullOrEmpty(Column);

    public bool IsColumn(string column) =>
        string.Equals(Column, column, StringComparison.Ordinal);

    public void Toggle(string column)
    {
        if (IsColumn(column))
        {
            Ascending = !Ascending;
            return;
        }

        Column = column;
        Ascending = true;
    }

    /// <summary>WAI-ARIA aria-sort value for a header cell.</summary>
    public string AriaSort(string column)
    {
        if (!IsColumn(column))
        {
            return "none";
        }

        return Ascending ? "ascending" : "descending";
    }

    /// <summary>Visual cue in the header: inactive, ascending, or descending.</summary>
    public string Indicator(string column)
    {
        if (!IsColumn(column))
        {
            return "↕";
        }

        return Ascending ? "↑" : "↓";
    }

    public string Hint(string column) =>
        !IsColumn(column)
            ? "Click to sort this column."
            : Ascending
                ? "Sorted ascending. Click to sort descending."
                : "Sorted descending. Click to sort ascending.";
}
