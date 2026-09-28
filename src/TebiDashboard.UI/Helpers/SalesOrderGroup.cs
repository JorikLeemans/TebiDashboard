using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Sales-order header used on the customer page, with the open lines that belong to it.
/// </summary>
public sealed class SalesOrderGroup
{
    public string Key { get; init; } = string.Empty;

    public string OrderLabel { get; init; } = string.Empty;

    public DateOnly Date { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal TotalQuantityOpen { get; init; }

    public string Status { get; init; } = string.Empty;

    public IReadOnlyList<OpenTransactionDto> Lines { get; init; } = [];
}

/// <summary>
/// Groups open sales-order or purchase-order lines by parent order so a detail page can show one row per order.
/// </summary>
public static class SalesOrderGroups
{
    /// <summary>
    /// Builds order groups newest-first. Composite keys "{orderId}-{lineId}" collapse under the order.
    /// </summary>
    public static IReadOnlyList<SalesOrderGroup> From(IEnumerable<OpenTransactionDto> rows) =>
        rows
            .GroupBy(GroupKey)
            .Select(group =>
            {
                var lines = group
                    .OrderByDescending(line => line.Amount)
                    .ThenBy(line => line.ItemName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new SalesOrderGroup
                {
                    Key = group.Key,
                    OrderLabel = group.Key,
                    Date = group.Min(line => line.TransactionDate),
                    TotalAmount = group.Sum(line => line.Amount),
                    TotalQuantityOpen = group.Sum(line => line.QuantityOpen),
                    Status = lines[0].Status,
                    Lines = lines
                };
            })
            .OrderByDescending(group => group.Date)
            .ThenBy(group => group.OrderLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Use the parent order id shown in the table. NetSuite stores "{transaction}-{line}".
    /// </summary>
    public static string GroupKey(OpenTransactionDto row) => SalesOrderLabel.For(row);
}
