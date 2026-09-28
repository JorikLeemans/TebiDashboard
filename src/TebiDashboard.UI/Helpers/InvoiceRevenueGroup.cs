using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Invoice header used on the customer revenue table, with the lines that belong to it.
/// </summary>
public sealed class InvoiceRevenueGroup
{
    public string Key { get; init; } = string.Empty;

    public string InvoiceLabel { get; init; } = string.Empty;

    public DateOnly Date { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<RevenueDto> Lines { get; init; } = [];
}

/// <summary>
/// Groups revenue lines by parent invoice so the customer page can show one row per invoice.
/// </summary>
public static class InvoiceRevenueGroups
{
    /// <summary>
    /// Builds invoice groups newest-first. Lines without an invoice id stay in their own group.
    /// </summary>
    public static IReadOnlyList<InvoiceRevenueGroup> From(IEnumerable<RevenueDto> rows) =>
        rows
            .GroupBy(GroupKey)
            .Select(group =>
            {
                var lines = group
                    .OrderByDescending(line => line.Amount)
                    .ThenBy(line => line.ItemName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var first = lines[0];

                return new InvoiceRevenueGroup
                {
                    Key = group.Key,
                    InvoiceLabel = InvoiceLabel.For(first),
                    Date = group.Min(line => line.Date),
                    TotalAmount = group.Sum(line => line.Amount),
                    Lines = lines
                };
            })
            .OrderByDescending(group => group.Date)
            .ThenBy(group => group.InvoiceLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Use the same invoice text the table shows. Older synced rows have empty InvoiceId /
    /// InvoiceNumber and only a "{invoice}-{line}" key, so grouping by row id would split
    /// one invoice across many header rows. A new incremental sync does not rewrite those rows.
    /// </summary>
    public static string GroupKey(RevenueDto row) => InvoiceLabel.For(row);
}
