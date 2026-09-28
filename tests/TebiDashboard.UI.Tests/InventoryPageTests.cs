using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Pages;
using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

public class InventoryPageTests
{
    [Fact]
    public void InventoryPage_ShowsItems_WhenApiReturnsData()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/inventory/items*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create<IReadOnlyList<InventoryItemDto>>(
            [
                new()
                {
                    ItemId = "i1",
                    ItemName = "Betonstraatstenen Antraciet",
                    Barcode = "8712345000011",
                    QuantityOnHand = 450,
                    ReorderPoint = 100,
                    RecentSalesVolume = 980,
                    IsLowStock = false
                }
            ]));

        var cut = ctx.Render<InventoryPage>();
        cut.WaitForElement(".inventory-table", TimeSpan.FromSeconds(5));

        Assert.Contains("Betonstraatstenen Antraciet", cut.Markup);
        Assert.Contains("8712345000011", cut.Markup);
    }

    [Fact]
    public void ItemDetailPage_ShowsStockSections_WhenApiReturnsData()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/items/i1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new ItemDetailDto
            {
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                Barcode = "8712345000011",
                TotalQuantityOnHand = 450,
                ReorderPoint = 100,
                RecentSalesVolume = 980,
                IsLowStock = false,
                Locations =
                [
            new() { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300, ReorderPoint = 50, PreferredStockLevel = 80 }
                ],
                OpenTransactionCount = 1,
                OpenTransactions =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "t1",
                        Type = "Sales Order",
                        ItemId = "i1",
                        ItemName = "Betonstraatstenen Antraciet",
                        TransactionDate = new DateOnly(2026, 6, 9),
                        QuantityOpen = 40,
                        Status = "Pending Fulfillment"
                    }
                ]
            }));

        var cut = ctx.Render<ItemDetailPage>(parameters => parameters.Add(p => p.ItemId, "i1"));
        cut.WaitForElement(".item-detail__header", TimeSpan.FromSeconds(5));

        Assert.Contains("Betonstraatstenen Antraciet", cut.Markup);
        Assert.Contains("Stock by location", cut.Markup);
        Assert.Contains("Open transactions", cut.Markup);
        Assert.Contains("Hoofdmagazijn", cut.Markup);
        Assert.Contains("Preferred stock", cut.Markup);
        var locations = cut.Find("table[aria-label='Stock locations']").TextContent;
        Assert.Contains("50", locations);
        Assert.Contains("80", locations);
        var status = cut.Find(".transaction-status");
        Assert.Equal("B", status.TextContent);
        Assert.Equal("Pending Fulfillment", status.GetAttribute("title"));
    }

    [Fact]
    public void ItemDetailPage_HidesNumericLocationIds_AndGroupsOpenOrderLines()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/items/i1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new ItemDetailDto
            {
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                Barcode = "8712345000011",
                TotalQuantityOnHand = 450,
                Locations =
                [
                    new StockLocationDto { LocationId = "16", LocationName = "16", QuantityOnHand = 300 },
                    new StockLocationDto { LocationId = "loc2", LocationName = "Terrein A", QuantityOnHand = 150 }
                ],
                OpenTransactionCount = 2,
                OpenTransactions =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "2136821-1",
                        Type = "Sales Order",
                        ItemId = "i1",
                        ItemName = "Betonstraatstenen Antraciet",
                        CustomerId = "c1",
                        CustomerName = "Bouwbedrijf Jansen",
                        TransactionDate = new DateOnly(2026, 6, 9),
                        QuantityOpen = 40,
                        Status = "B"
                    },
                    new OpenTransactionDto
                    {
                        TransactionId = "2136821-2",
                        Type = "Sales Order",
                        ItemId = "i1",
                        ItemName = "Betonstraatstenen Antraciet",
                        CustomerId = "c1",
                        CustomerName = "Bouwbedrijf Jansen",
                        TransactionDate = new DateOnly(2026, 6, 9),
                        QuantityOpen = 12,
                        Status = "B"
                    }
                ]
            }));

        var cut = ctx.Render<ItemDetailPage>(parameters => parameters.Add(p => p.ItemId, "i1"));
        cut.WaitForElement(".item-detail__header", TimeSpan.FromSeconds(5));

        var locations = cut.Find("table[aria-label='Stock locations']").TextContent;
        Assert.Contains("Terrein A", locations);
        Assert.DoesNotContain("16", locations);

        var transactions = cut.Find("table[aria-label='Open transactions']");
        Assert.Contains("2136821", transactions.TextContent);
        Assert.DoesNotContain("2136821-1", transactions.TextContent);
        Assert.Single(cut.FindAll("table[aria-label='Open transactions'] tbody .item-detail__order-row"));

        cut.Find("table[aria-label='Open transactions'] .item-detail__order-row").Click();
        Assert.Contains("40 open", cut.Find("table[aria-label='Open transactions']").TextContent);
        Assert.Contains("12 open", cut.Find("table[aria-label='Open transactions']").TextContent);
    }

    private static BunitContext CreateAuthenticatedContext(out MockHttpMessageHandler mockHttp)
    {
        var ctx = new BunitContext();
        mockHttp = new MockHttpMessageHandler();

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton(new DashboardApiClient(client));
        ctx.Services.AddSingleton<IAuthStateService>(new FakeAuthStateService(isAuthenticated: true));
        ctx.Services.AddSingleton<IPeriodFilterService>(new PeriodFilterService());

        return ctx;
    }

    private sealed class FakeAuthStateService(bool isAuthenticated) : IAuthStateService
    {
        public bool IsAuthenticated { get; } = isAuthenticated;

        public string? Token => isAuthenticated ? "test-token" : null;

        public string? Username => isAuthenticated ? "demo" : null;

        public string? Role => isAuthenticated ? "Management" : null;

        public event Action? OnChange;

        public bool CanAccessAdminDashboard() => isAuthenticated && Role == Roles.Admin;

        public bool CanManageGoals() => isAuthenticated && Role == Roles.Management;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
