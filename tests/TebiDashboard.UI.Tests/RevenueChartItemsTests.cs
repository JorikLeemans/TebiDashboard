using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public sealed class RevenueChartItemsTests
{
    [Fact]
    public void FromDailyTotals_SumsRevenuePerDay()
    {
        IReadOnlyList<RevenueDto> revenue =
        [
            new() { Id = "1", Date = new DateOnly(2026, 6, 9), Amount = 10_000m, CustomerName = "A" },
            new() { Id = "2", Date = new DateOnly(2026, 6, 9), Amount = 5_000m, CustomerName = "B" },
            new() { Id = "3", Date = new DateOnly(2026, 6, 10), Amount = 7_500m, CustomerName = "C" }
        ];

        var items = RevenueChartItems.FromDailyTotals(revenue);

        Assert.Equal(2, items.Count);
        Assert.Equal(15_000m, items[0].Value);
        Assert.Equal(7_500m, items[1].Value);
    }
}
