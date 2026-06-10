using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TebbiDashboard.Api.Tests;

public class HealthEndpointTests : IClassFixture<TebbiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TebbiWebApplicationFactory factory)
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

    [Fact]
    public async Task InventoryItems_ReturnsOk_WithMockData()
    {
        var response = await _client.GetAsync("/api/inventory/items");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ItemById_ReturnsOk_ForKnownItem()
    {
        var response = await _client.GetAsync("/api/items/i1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ItemById_ReturnsNotFound_ForUnknownItem()
    {
        var response = await _client.GetAsync("/api/items/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
