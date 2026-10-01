using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// One invoice on the item page, with the lines of this item that belong to it.
/// </summary>
public sealed class ItemInvoiceGroup
{
    public string Key { get; init; } = string.Empty;

    public string InvoiceLabel { get; init; } = string.Empty;

    public DateOnly Date { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal TotalUnits { get; init; }

    public IReadOnlyList<ItemInvoiceLineDto> Lines { get; init; } = [];
}

/// <summary>
/// Groups an item's period invoice lines the same way the home and customer tables group revenue.
/// </summary>
public static class ItemInvoiceGroups
{
    /// <summary>
    /// One row per invoice, newest first. Lines of the same invoice stay together.
    /// </summary>
    public static IReadOnlyList<ItemInvoiceGroup> From(IEnumerable<ItemInvoiceLineDto> rows) =>
        rows
            .GroupBy(InvoiceLabel)
            .Select(group =>
            {
                var lines = group
                    .OrderByDescending(line => line.Amount)
                    .ThenByDescending(line => line.Date)
                    .ToList();

                return new ItemInvoiceGroup
                {
                    Key = group.Key,
                    InvoiceLabel = group.Key,
                    Date = group.Min(line => line.Date),
                    TotalAmount = group.Sum(line => line.Amount),
                    TotalUnits = group.Sum(line => line.UnitsSold),
                    Lines = lines
                };
            })
            .OrderByDescending(group => group.Date)
            .ThenBy(group => group.InvoiceLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Document number when NetSuite sent one, otherwise the internal invoice id.</summary>
    public static string InvoiceLabel(ItemInvoiceLineDto line) =>
        !string.IsNullOrWhiteSpace(line.InvoiceNumber) ? line.InvoiceNumber : line.InvoiceId;
}
