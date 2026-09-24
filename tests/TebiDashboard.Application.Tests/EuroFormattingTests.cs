using TebiDashboard.Shared.Formatting;

namespace TebiDashboard.Application.Tests;

public class EuroFormattingTests
{
    [Fact]
    public void FormatWhole_UsesEuroSymbol()
    {
        var formatted = EuroFormatting.FormatWhole(42000m);

        Assert.Contains('€', formatted);
        Assert.DoesNotContain('¤', formatted);
    }

    [Fact]
    public void Format_UsesEuroSymbolAndTwoDecimals()
    {
        var formatted = EuroFormatting.Format(42000.5m);

        Assert.Contains('€', formatted);
        Assert.Contains(",50", formatted);
        Assert.DoesNotContain('¤', formatted);
    }
}
