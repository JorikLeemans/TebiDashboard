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
}
