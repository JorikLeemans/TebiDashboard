using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

public class PeriodFilterServiceTests
{
    [Fact]
    public void ApplyPreset_ThisYear_SetsRangeFromJanuaryFirstToToday()
    {
        var service = new PeriodFilterService();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        service.ApplyPreset(PeriodFilterPreset.ThisYear);

        Assert.Equal(new DateOnly(today.Year, 1, 1), service.From);
        Assert.Equal(today, service.To);
    }
}
