using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Components;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Builds chart series from revenue rows grouped by calendar day.
/// </summary>
public static class RevenueChartItems
{
    public static IReadOnlyList<ChartBarItem> FromDailyTotals(IEnumerable<RevenueDto> revenue) =>
        revenue
            .GroupBy(row => row.Date)
            .OrderBy(group => group.Key)
            .Select(group => new ChartBarItem
            {
                Label = group.Key.ToString("d"),
                Value = group.Sum(row => row.Amount)
            })
            .ToList();
}
