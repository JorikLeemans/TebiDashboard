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
                    new StockLocationDto { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300 }
                ],
                OpenTransactionCount = 1,
                OpenTransactions =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "t1",
                        Type = "Sales Order",
                        ItemId = "i1",
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
    }

    private static BunitContext CreateAuthenticatedContext(out MockHttpMessageHandler mockHttp)
    {
        var ctx = new BunitContext();
        mockHttp = new MockHttpMessageHandler();

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton(new DashboardApiClient(client));
        ctx.Services.AddSingleton<IAuthStateService>(new FakeAuthStateService(isAuthenticated: true));

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

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
