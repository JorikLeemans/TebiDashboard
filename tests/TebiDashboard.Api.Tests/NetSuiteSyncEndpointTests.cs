using System.Net;
using System.Net.Http.Json;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Tests;

/// <summary>
/// NetSuite sync endpoint authorization checks.
/// </summary>
public sealed class NetSuiteSyncEndpointTests
{
    [Fact]
    public async Task Sync_RequiresAdminRole()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/netsuite/sync", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Sync_StartsInBackground_AndEventuallyFails_WhenNetSuiteIsNotConfigured()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tebi-Role", "Admin");

        var response = await client.PostAsync("/api/netsuite/sync", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        NetSuiteSyncStatusDto? finalStatus = null;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var statusResponse = await client.GetAsync("/api/netsuite/sync/status");
            finalStatus = await statusResponse.Content.ReadFromJsonAsync<NetSuiteSyncStatusDto>();
            Assert.NotNull(finalStatus);

            if (finalStatus.State is not "Running")
            {
                break;
            }

            await Task.Delay(100);
        }

        Assert.NotNull(finalStatus);
        Assert.Equal("Failed", finalStatus.State);
        Assert.Contains("PostgreSQL", finalStatus.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sync_AcceptsLookbackDaysQuery()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tebi-Role", "Admin");

        var response = await client.PostAsync("/api/netsuite/sync?lookbackDays=14", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<NetSuiteSyncStatusDto>();
        Assert.NotNull(status);
        Assert.Equal(14, status.LookbackDays);
    }

    [Fact]
    public async Task Setup_RequiresAdminRole()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/netsuite/setup");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Setup_ReportsNotConfigured_AndIdleLastSync_OnDefaultTestHost()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tebi-Role", "Admin");

        var response = await client.GetAsync("/api/netsuite/setup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var setup = await response.Content.ReadFromJsonAsync<NetSuiteSetupStatusDto>();
        Assert.NotNull(setup);
        Assert.False(setup.IsConfigured);
        Assert.False(setup.CanTestConnection);
        Assert.False(setup.CanSync);
        Assert.Contains("Enabled", setup.MissingSettings);
        Assert.Contains("demo", setup.DataSourceMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Idle", setup.LastSync.State);
        Assert.Contains("No successful NetSuite import", setup.LastSync.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, setup.LookbackDaysMin);
        Assert.Equal(730, setup.LookbackDaysMax);
        Assert.Equal(365, setup.SuggestedLookbackDays);
    }
}
