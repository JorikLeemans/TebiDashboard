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
                    UnitsSold = 420,
                    IsLowStock = false
                }
            ]));

        var cut = ctx.Render<InventoryPage>();
        cut.WaitForElement(".inventory-table", TimeSpan.FromSeconds(5));

        Assert.Contains("Betonstraatstenen Antraciet", cut.Markup);
        Assert.Contains("Sold in period", cut.Markup);
        Assert.Contains("Units sold", cut.Markup);
        Assert.Contains("420", cut.Markup);
        Assert.Contains("8712345000011", cut.Markup);
    }

    [Fact]
    public void TopSellingPage_ShowsPeriodAmount_AndFiltersByProductGroup()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        string? requestedQuery = null;
        mockHttp.When(HttpMethod.Get, "*/api/sales-volume/top*")
            .Respond(request =>
            {
                requestedQuery = request.RequestUri?.Query;
                var groupId = request.RequestUri?.Query.Contains("productGroupId=pg-tiles", StringComparison.Ordinal) == true
                    ? "pg-tiles"
                    : null;
                IReadOnlyList<TopSellingItemDto> items = groupId is null
                    ?
                    [
                        new TopSellingItemDto
                        {
                            ItemId = "i1",
                            ItemName = "Betonstraatstenen Antraciet",
                            ProductGroupId = "pg-paving",
                            ProductGroupName = "Bestrating",
                            SalesAmount = 12_500m,
                            UnitsSold = 40m
                        }
                    ]
                    : [];

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new TopSellingItemsResultDto
                    {
                        Items = items,
                        ProductGroups =
                        [
                            new ProductGroupOptionDto { Id = "pg-paving", Name = "Bestrating" },
                            new ProductGroupOptionDto { Id = "pg-tiles", Name = "Tegels" }
                        ]
                    })
                };
            });

        var cut = ctx.Render<TopSellingPage>();
        cut.WaitForElement("table[aria-label='Bestsellers']", TimeSpan.FromSeconds(5));

        Assert.Contains("Betonstraatstenen Antraciet", cut.Markup);
        Assert.Contains("Bestrating", cut.Markup);
        Assert.Contains("12.500", cut.Markup);
        Assert.Contains("Units sold", cut.Markup);
        Assert.Contains("40", cut.Markup);
        Assert.Contains("from=", requestedQuery);

        cut.Find("select").Change("pg-tiles");
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("productGroupId=pg-tiles", requestedQuery);
            Assert.Contains("No sales in this period", cut.Markup);
        });
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
                PeriodSalesAmount = 12_500m,
                PeriodInvoices =
                [
                    new ItemInvoiceLineDto
                    {
                        Date = new DateOnly(2026, 6, 2),
                        InvoiceNumber = "F-2026-014",
                        CustomerId = "c1",
                        CustomerName = "Bouwbedrijf Jansen",
                        Amount = 8_000m,
                        UnitsSold = 20m
                    },
                    new ItemInvoiceLineDto
                    {
                        Date = new DateOnly(2026, 6, 8),
                        InvoiceNumber = "F-2026-018",
                        CustomerId = "c2",
                        CustomerName = "Gemeente Utrecht",
                        Amount = 4_500m,
                        UnitsSold = 12m
                    }
                ],
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
        Assert.Contains("Sold in period", cut.Markup);
        Assert.Contains("12.500", cut.Markup);
        Assert.Contains("Invoices in period", cut.Markup);
        Assert.Contains("F-2026-014", cut.Markup);
        Assert.Contains("Bouwbedrijf Jansen", cut.Markup);
        Assert.Contains("/customers/c1", cut.Markup);
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
    public void ItemDetailPage_GroupsInvoiceLines_AndExpandsThemOnClick()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/items/i1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new ItemDetailDto
            {
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                PeriodSalesAmount = 9_000m,
                PeriodInvoices =
                [
                    new ItemInvoiceLineDto
                    {
                        Date = new DateOnly(2026, 6, 2),
                        InvoiceNumber = "F-2026-014",
                        CustomerId = "c1",
                        CustomerName = "Bouwbedrijf Jansen",
                        Amount = 8_000m,
                        UnitsSold = 20m
                    },
                    new ItemInvoiceLineDto
                    {
                        Date = new DateOnly(2026, 6, 2),
                        InvoiceNumber = "F-2026-014",
                        CustomerId = "c1",
                        CustomerName = "Bouwbedrijf Jansen",
                        Amount = 1_000m,
                        UnitsSold = 4m
                    }
                ]
            }));

        var cut = ctx.Render<ItemDetailPage>(parameters => parameters.Add(p => p.ItemId, "i1"));
        cut.WaitForElement("table[aria-label='Invoices in period']", TimeSpan.FromSeconds(5));

        var table = cut.Find("table[aria-label='Invoices in period']");
        Assert.Single(table.QuerySelectorAll("tbody tr"));

        table.QuerySelector("tbody tr")!.Click();

        Assert.Equal(3, cut.FindAll("table[aria-label='Invoices in period'] tbody tr").Count);
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

        cut.Find("table[aria-label='Open transactions'] .item-detail__order-toggle").Click();
        Assert.Contains("40 open", cut.Find("table[aria-label='Open transactions']").TextContent);
        Assert.Contains("12 open", cut.Find("table[aria-label='Open transactions']").TextContent);
    }

    [Fact]
    public void ItemDetailPage_OpenTransactionRow_IsClickableLinkToCustomer()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/items/i1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new ItemDetailDto
            {
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                OpenTransactionCount = 1,
                OpenTransactions =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "t1",
                        Type = "Sales Order",
                        ItemId = "i1",
                        ItemName = "Betonstraatstenen Antraciet",
                        CustomerId = "c1",
                        CustomerName = "Bouwbedrijf Jansen",
                        TransactionDate = new DateOnly(2026, 6, 9),
                        QuantityOpen = 40,
                        Status = "Pending Fulfillment"
                    }
                ]
            }));

        var cut = ctx.Render<ItemDetailPage>(parameters => parameters.Add(p => p.ItemId, "i1"));
        cut.WaitForElement("table[aria-label='Open transactions']", TimeSpan.FromSeconds(5));

        Assert.Contains("item-detail__order-row--link", cut.Markup);
        Assert.Contains("Bouwbedrijf Jansen", cut.Markup);
        Assert.Contains("role=\"link\"", cut.Markup);
    }

    [Fact]
    public void ItemDetailPage_OpenTransactionRow_IsClickableLinkToSupplier()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/items/i4*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new ItemDetailDto
            {
                ItemId = "i4",
                ItemName = "Betonblokken 40x20",
                OpenTransactionCount = 1,
                OpenTransactions =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "2136801-1",
                        Type = "Purchase Order",
                        ItemId = "i4",
                        ItemName = "Betonblokken 40x20",
                        SupplierId = "s1",
                        SupplierName = "BetonCentrale Midden-NL",
                        TransactionDate = new DateOnly(2026, 6, 9),
                        QuantityOpen = 45,
                        Status = "Pending Receipt"
                    }
                ]
            }));

        var cut = ctx.Render<ItemDetailPage>(parameters => parameters.Add(p => p.ItemId, "i4"));
        cut.WaitForElement("table[aria-label='Open transactions']", TimeSpan.FromSeconds(5));

        Assert.Contains("item-detail__order-row--link", cut.Markup);
        Assert.Contains("BetonCentrale Midden-NL", cut.Markup);
    }

    [Fact]
    public void ItemDetailPage_TransferOrderRow_IsNotAPartyLink()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/items/i1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new ItemDetailDto
            {
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                OpenTransactionCount = 1,
                OpenTransactions =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "t2",
                        Type = "Transfer Order",
                        ItemId = "i1",
                        TransactionDate = new DateOnly(2026, 6, 8),
                        QuantityOpen = 20,
                        Status = "Pending Receipt"
                    }
                ]
            }));

        var cut = ctx.Render<ItemDetailPage>(parameters => parameters.Add(p => p.ItemId, "i1"));
        cut.WaitForElement("table[aria-label='Open transactions']", TimeSpan.FromSeconds(5));

        Assert.DoesNotContain("item-detail__order-row--link", cut.Markup);
        Assert.Contains("Transfer Order", cut.Markup);
    }

    private static BunitContext CreateAuthenticatedContext(out MockHttpMessageHandler mockHttp)
    {
        var ctx = new BunitContext();
        mockHttp = new MockHttpMessageHandler();

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        var auth = new FakeAuthStateService(isAuthenticated: true);
        ctx.Services.AddSingleton(new DashboardApiClient(client, auth));
        ctx.Services.AddSingleton<IAuthStateService>(auth);
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
