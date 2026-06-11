using Bunit;
using TebiDashboard.UI.Components;

namespace TebiDashboard.UI.Tests;

public class BarChartTests
{
    [Fact]
    public void BarChart_RendersScaledBarHeights_BasedOnRelativeValues()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<BarChart>(parameters => parameters
            .Add(p => p.Items,
            [
                new ChartBarItem { Label = "Q1", Value = 500_000m },
                new ChartBarItem { Label = "Q2", Value = 250_000m },
                new ChartBarItem { Label = "Q3", Value = 125_000m }
            ])
            .Add(p => p.HeightPx, 220));

        cut.WaitForElement(".bar-chart__bar");

        Assert.Contains("height: 100.0%", cut.Markup);
        Assert.Contains("height: 50.0%", cut.Markup);
        Assert.Contains("height: 25.0%", cut.Markup);
    }
}
