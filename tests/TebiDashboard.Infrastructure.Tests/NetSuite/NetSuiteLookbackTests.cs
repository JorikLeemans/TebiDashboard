using TebiDashboard.Shared.Constants;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Admin slider and API must agree on lookback bounds and the "days since last success" suggestion.
/// </summary>
public sealed class NetSuiteLookbackTests
{
    [Fact]
    public void Clamp_LimitsToOneThroughTwoYears()
    {
        Assert.Equal(NetSuiteLookback.MinDays, NetSuiteLookback.Clamp(0));
        Assert.Equal(NetSuiteLookback.MaxDays, NetSuiteLookback.Clamp(10_000));
        Assert.Equal(45, NetSuiteLookback.Clamp(45));
    }

    [Fact]
    public void Suggest_UsesFullYear_WhenNeverSynced()
    {
        Assert.Equal(NetSuiteLookback.FirstImportDays, NetSuiteLookback.Suggest(null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Suggest_AddsBuffer_AfterRecentSuccess()
    {
        var now = DateTimeOffset.Parse("2026-09-28T10:00:00Z");
        var last = now.AddDays(-2);

        Assert.Equal(NetSuiteLookback.DefaultDays, NetSuiteLookback.Suggest(last, now));
    }
}
