using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public sealed class PeriodFilterReloadTests
{
    [Fact]
    public void BeginReload_InvalidatesPreviousGeneration()
    {
        var reload = new PeriodFilterReload();

        var firstGeneration = reload.BeginReload(out _);
        var secondGeneration = reload.BeginReload(out _);

        Assert.False(reload.IsCurrent(firstGeneration));
        Assert.True(reload.IsCurrent(secondGeneration));
    }
}
