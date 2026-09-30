using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Tests;

public class TopSellingEndpointTests : IClassFixture<TebiWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;

    public TopSellingEndpointTests(TebiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => TestAuth.SignInAsync(_client);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TopSelling_ReturnsLimitedItems_AndKeepsProductGroups()
    {
        var response = await _client.GetAsync(
            "/api/sales-volume/top?from=2026-01-01&to=2026-09-30&limit=1&productGroupId=pg-paving");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TopSellingItemsResultDto>();
        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.Equal("i1", body.Items[0].ItemId);
        Assert.Equal(98_000m, body.Items[0].SalesAmount);
        Assert.Contains(body.ProductGroups, group => group.Id == "pg-paving");
    }

    [Fact]
    public async Task TopSelling_UnknownProductGroup_ReturnsNoItems()
    {
        var response = await _client.GetAsync(
            "/api/sales-volume/top?from=2026-01-01&to=2026-09-30&productGroupId=missing");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TopSellingItemsResultDto>();
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.NotEmpty(body.ProductGroups);
    }

    [Fact]
    public async Task Popular_ReturnsAShortList_WithoutProductGroups()
    {
        var response = await _client.GetAsync("/api/sales-volume/popular?from=2026-01-01&to=2026-09-30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("productGroups", json, StringComparison.OrdinalIgnoreCase);
        var items = System.Text.Json.JsonSerializer.Deserialize<List<TopSellingItemDto>>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(items);
        Assert.InRange(items.Count, 1, 5);
        Assert.Equal("i1", items[0].ItemId);
        Assert.Equal(98_000m, items[0].SalesAmount);
        Assert.Equal(420m, items[0].UnitsSold);
    }
}
