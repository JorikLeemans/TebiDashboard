using Microsoft.EntityFrameworkCore;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Last successful sync time must survive a new DbContext (API restart).
/// </summary>
public sealed class EfNetSuiteSyncCheckpointStoreTests
{
    [Fact]
    public async Task SaveAsync_PersistsLastSucceededAt()
    {
        var options = new DbContextOptionsBuilder<TebiDashboardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var succeededAt = DateTimeOffset.Parse("2026-09-27T12:00:00Z");
        await using (var db = new TebiDashboardDbContext(options))
        {
            var store = new EfNetSuiteSyncCheckpointStore(db);
            await store.SaveAsync(new NetSuiteSyncCheckpoint(
                succeededAt,
                succeededAt,
                30,
                "Succeeded",
                "ok"));
        }

        await using (var db = new TebiDashboardDbContext(options))
        {
            var store = new EfNetSuiteSyncCheckpointStore(db);
            var loaded = await store.GetAsync();
            Assert.Equal(succeededAt, loaded.LastSucceededAt);
            Assert.Equal(30, loaded.LastLookbackDays);
            Assert.Equal("Succeeded", loaded.LastState);
        }
    }
}
