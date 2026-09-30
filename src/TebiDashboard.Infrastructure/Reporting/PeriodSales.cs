using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Infrastructure.Reporting;

/// <summary>
/// One invoice line used to rank items by sales amount in a period.
/// </summary>
internal readonly record struct PeriodSaleLine(
    string ItemId,
    string ItemName,
    string ProductGroupId,
    string ProductGroupName,
    decimal Amount,
    decimal Quantity);

/// <summary>
/// Groups invoice lines into a top-N list and the product groups available in that period.
/// </summary>
internal static class PeriodSales
{
    public static TopSellingItemsResultDto Rank(
        IReadOnlyList<PeriodSaleLine> lines,
        int limit,
        string? productGroupId)
    {
        var withItems = lines
            .Where(line => !string.IsNullOrWhiteSpace(line.ItemId))
            .ToList();

        var groups = withItems
            .Where(line => !string.IsNullOrWhiteSpace(line.ProductGroupId))
            .GroupBy(line => line.ProductGroupId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProductGroupOptionDto
            {
                Id = group.Key,
                Name = FirstText(group.Select(line => line.ProductGroupName), group.Key)
            })
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var filtered = string.IsNullOrWhiteSpace(productGroupId)
            ? withItems
            : withItems
                .Where(line => string.Equals(line.ProductGroupId, productGroupId, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var items = filtered
            .GroupBy(line => line.ItemId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TopSellingItemDto
            {
                ItemId = group.Key,
                ItemName = FirstText(group.Select(line => line.ItemName), group.Key),
                ProductGroupId = FirstText(group.Select(line => line.ProductGroupId), string.Empty),
                ProductGroupName = FirstText(
                    group.Select(line => line.ProductGroupName),
                    FirstText(group.Select(line => line.ProductGroupId), string.Empty)),
                SalesAmount = group.Sum(line => line.Amount),
                UnitsSold = group.Sum(line => line.Quantity)
            })
            .OrderByDescending(item => item.SalesAmount)
            .ThenBy(item => item.ItemName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, limit))
            .ToList();

        return new TopSellingItemsResultDto
        {
            Items = items,
            ProductGroups = groups
        };
    }

    private static string FirstText(IEnumerable<string?> values, string fallback) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? fallback;
}
