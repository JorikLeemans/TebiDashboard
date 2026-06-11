using Bunit;
using TebiDashboard.UI.Components;

namespace TebiDashboard.UI.Tests;

public class LineChartTests
{
    [Fact]
    public void LineChart_RendersSmoothLineWithYAxis_WhenItemsProvided()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<LineChart>(parameters => parameters
            .Add(p => p.Items,
            [
                new ChartBarItem { Label = "01-04-2026", Value = 33_600m },
                new ChartBarItem { Label = "15-04-2026", Value = 55_400m },
                new ChartBarItem { Label = "01-05-2026", Value = 8_200m },
                new ChartBarItem { Label = "10-06-2026", Value = 42_000m }
            ])
            .Add(p => p.AriaLabel, "Daily revenue trend"));

        cut.WaitForElement(".line-chart__svg");

        Assert.Contains("line-chart__line", cut.Markup);
        Assert.Contains("line-chart-clip-", cut.Markup);
        Assert.Contains("clip-path", cut.Markup);
        Assert.Contains("line-chart__y-label", cut.Markup);
        Assert.Contains("line-chart__grid", cut.Markup);
        Assert.Contains("01-04-2026", cut.Markup);
    }

    [Fact]
    public void LineChart_ShowsEmptyMessage_WhenNoItems()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<LineChart>(parameters => parameters
            .Add(p => p.EmptyMessage, "No revenue data"));

        Assert.Contains("No revenue data", cut.Markup);
    }
}
