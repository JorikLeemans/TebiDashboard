using TebiDashboard.UI.Helpers;
using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

public sealed class PeriodFilterSubscriptionTests
{
    [Fact]
    public void Bind_InvokesHandler_WhenPeriodChanges()
    {
        var service = new PeriodFilterService();
        var subscription = new PeriodFilterSubscription();
        var changeCount = 0;

        subscription.Bind(service, service, () => changeCount++);
        service.ApplyPreset(PeriodFilterPreset.Last7Days);

        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void Bind_Rebinds_WhenServiceInstanceChanges()
    {
        var first = new PeriodFilterService();
        var second = new PeriodFilterService();
        var subscription = new PeriodFilterSubscription();
        var changes = new List<string>();

        subscription.Bind(first, first, () => changes.Add("first"));
        subscription.Bind(second, second, () => changes.Add("second"));

        first.ApplyPreset(PeriodFilterPreset.Last7Days);
        second.ApplyPreset(PeriodFilterPreset.Last30Days);

        Assert.Equal(["second"], changes);
    }
}
