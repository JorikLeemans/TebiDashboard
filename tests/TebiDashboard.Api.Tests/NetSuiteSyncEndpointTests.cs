using System.Net;
using System.Net.Http.Json;

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
    public async Task Sync_ReturnsBadRequest_WhenNetSuiteIsNotConfigured()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tebi-Role", "Admin");

        var response = await client.PostAsync("/api/netsuite/sync", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SyncResponse>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Contains("PostgreSQL", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SyncResponse
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
