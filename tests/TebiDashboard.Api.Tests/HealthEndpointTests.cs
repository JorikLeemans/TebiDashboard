using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TebiDashboard.Api.Tests;

public class HealthEndpointTests : IClassFixture<TebiWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TebiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => TestAuth.SignInAsync(_client);

    public Task DisposeAsync() => Task.CompletedTask;

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
    public async Task Goals_Save_ReturnsOk_ForValidTargets()
    {
        var year = DateTime.UtcNow.Year;
        var response = await _client.PutAsJsonAsync("/api/goals", new
        {
            year,
            targets = new[]
            {
                new { period = "YEAR", itemId = (string?)null, targetValue = 2_050_000m },
                new { period = "Q3", itemId = "i1", targetValue = 1300m }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("YEAR", content);
        Assert.Contains("i1", content);
    }

    [Fact]
    public async Task ItemById_ReturnsNotFound_ForUnknownItem()
    {
        var response = await _client.GetAsync("/api/items/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
