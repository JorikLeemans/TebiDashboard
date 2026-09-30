using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// The admin success line includes how long the import ran.
/// </summary>
public sealed class SyncDurationTests
{
    [Fact]
    public void Append_AddsElapsedTime_ToTheSuccessMessage()
    {
        var started = DateTimeOffset.Parse("2026-09-30T06:40:00Z");
        var completed = started.AddMinutes(2).AddSeconds(5);

        var message = SyncDuration.Append("NetSuite data imported successfully.", started, completed);

        Assert.Equal("NetSuite data imported successfully. Completed in 2m 05s.", message);
    }

    [Fact]
    public void Append_LeavesTheMessage_WhenStartIsMissing()
    {
        var message = SyncDuration.Append("NetSuite data imported successfully.", null, DateTimeOffset.UtcNow);

        Assert.Equal("NetSuite data imported successfully.", message);
    }
}
