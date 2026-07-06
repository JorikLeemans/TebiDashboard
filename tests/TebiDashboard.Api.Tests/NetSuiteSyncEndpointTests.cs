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
}
