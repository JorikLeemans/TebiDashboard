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

        // Bar area = 220 - 22 - 26 = 172px; heights scale to 100%, 50%, and 25%.
        Assert.Contains("height: 172px", cut.Markup);
        Assert.Contains("height: 86px", cut.Markup);
        Assert.Contains("height: 43px", cut.Markup);
    }
}
