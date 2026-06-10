using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TebbiDashboard.Api.Tests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DashboardSummary_ReturnsOk_WithMockData()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ItemByBarcode_ReturnsNotFound_ForUnknownBarcode()
    {
        var response = await _client.GetAsync("/api/items/by-barcode/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
