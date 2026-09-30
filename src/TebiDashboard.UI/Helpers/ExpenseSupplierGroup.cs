using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Supplier header for the dashboard expenses table, with the bills that belong to it.
/// </summary>
public sealed class ExpenseSupplierGroup
{
    public string Key { get; init; } = string.Empty;

    public string? SupplierId { get; init; }

    public string SupplierLabel { get; init; } = string.Empty;

    public DateOnly Date { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<ExpenseDto> Lines { get; init; } = [];
}

/// <summary>
/// Groups expense rows by supplier so the dashboard shows one row per supplier, like invoices on a customer page.
/// </summary>
public static class ExpenseSupplierGroups
{
    /// <summary>
    /// Builds supplier groups newest-first. Bills without a supplier stay on their own row.
    /// </summary>
    public static IReadOnlyList<ExpenseSupplierGroup> From(IEnumerable<ExpenseDto> rows) =>
        rows
            .GroupBy(GroupKey)
            .Select(group =>
            {
                var lines = group
                    .OrderByDescending(line => line.Date)
                    .ThenByDescending(line => line.Amount)
                    .ToList();
                var first = lines[0];

                return new ExpenseSupplierGroup
                {
                    Key = group.Key,
                    SupplierId = string.IsNullOrWhiteSpace(first.SupplierId) ? null : first.SupplierId,
                    SupplierLabel = SupplierLabel(first),
                    Date = lines.Max(line => line.Date),
                    TotalAmount = group.Sum(line => line.Amount),
                    Lines = lines
                };
            })
            .OrderByDescending(group => group.Date)
            .ThenBy(group => group.SupplierLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Prefer the supplier master id. A bill with no supplier keeps its own id so unrelated rows do not merge.
    /// </summary>
    public static string GroupKey(ExpenseDto row) =>
        !string.IsNullOrWhiteSpace(row.SupplierId)
            ? row.SupplierId
            : row.Id;

    /// <summary>
    /// Readable supplier text. The id is shown separately in the table.
    /// </summary>
    public static string SupplierLabel(ExpenseDto expense)
    {
        if (!string.IsNullOrWhiteSpace(expense.SupplierName)
            && !string.Equals(expense.SupplierName, expense.SupplierId, StringComparison.OrdinalIgnoreCase))
        {
            return expense.SupplierName;
        }

        if (!string.IsNullOrWhiteSpace(expense.VendorName))
        {
            return expense.VendorName.Trim();
        }

        return expense.SupplierId ?? string.Empty;
    }
}
