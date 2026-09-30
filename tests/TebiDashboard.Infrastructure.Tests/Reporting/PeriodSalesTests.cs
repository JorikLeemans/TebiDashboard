using TebiDashboard.Infrastructure.Reporting;

namespace TebiDashboard.Infrastructure.Tests.Reporting;

public class PeriodSalesTests
{
    [Fact]
    public void Rank_OrdersByAmount_AndKeepsGroupsWhenFiltered()
    {
        var lines = new[]
        {
            new PeriodSaleLine("a", "Alpha", "g1", "Stenen", 10m),
            new PeriodSaleLine("a", "Alpha", "g1", "Stenen", 5m),
            new PeriodSaleLine("b", "Beta", "g2", "Tegels", 20m),
            new PeriodSaleLine("c", "", "g1", "Stenen", 100m)
        };

        var all = PeriodSales.Rank(lines, limit: 20, productGroupId: null);

        Assert.Equal(["c", "b", "a"], all.Items.Select(item => item.ItemId));
        Assert.Equal(100m, all.Items[0].SalesAmount);
        Assert.Equal("c", all.Items[0].ItemName);
        Assert.Equal(15m, all.Items.Single(item => item.ItemId == "a").SalesAmount);
        Assert.Equal(["Stenen", "Tegels"], all.ProductGroups.Select(group => group.Name));

        var filtered = PeriodSales.Rank(lines, limit: 20, productGroupId: "g2");

        Assert.Single(filtered.Items);
        Assert.Equal("b", filtered.Items[0].ItemId);
        Assert.Equal(2, filtered.ProductGroups.Count);
    }

    [Fact]
    public void Rank_RespectsLimit_AndSkipsLinesWithoutAnItem()
    {
        var lines = new[]
        {
            new PeriodSaleLine("", "Header only", "g1", "Stenen", 500m),
            new PeriodSaleLine("a", "Alpha", "g1", "Stenen", 10m),
            new PeriodSaleLine("b", "Beta", "g1", "Stenen", 20m)
        };

        var ranked = PeriodSales.Rank(lines, limit: 1, productGroupId: null);

        Assert.Single(ranked.Items);
        Assert.Equal("b", ranked.Items[0].ItemId);
    }
}
