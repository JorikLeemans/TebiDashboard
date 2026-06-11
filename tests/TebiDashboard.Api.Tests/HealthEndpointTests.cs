using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TebiDashboard.Api.Tests;

public class HealthEndpointTests : IClassFixture<TebiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TebiWebApplicationFactory factory)
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
    public async Task PeriodOverview_ReturnsOk_WithMockData()
    {
        var response = await _client.GetAsync("/api/dashboard/period-overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CustomersOverview_ReturnsCustomers_WithMockData()
    {
        var response = await _client.GetAsync("/api/customers/overview");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Bouwbedrijf Jansen", content);
    }

    [Fact]
    public async Task SuppliersOverview_ReturnsSuppliers_WithMockData()
    {
        var response = await _client.GetAsync("/api/suppliers/overview");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("BetonCentrale Midden-NL", content);
    }

    [Fact]
    public async Task Goals_ReturnsOk_WithPeriodFilter()
    {
        var response = await _client.GetAsync("/api/goals?year=2026&period=Q2");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Q2", content);
    }

    [Fact]
    public async Task Goals_ReturnsPriorYearData_ForComparison()
    {
        var priorYear = DateTime.UtcNow.Year - 1;
        var response = await _client.GetAsync($"/api/goals?year={priorYear}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Q1", content);
        Assert.Contains("Q4", content);
    }

    [Fact]
    public async Task ItemById_ReturnsNotFound_ForUnknownItem()
    {
        var response = await _client.GetAsync("/api/items/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
