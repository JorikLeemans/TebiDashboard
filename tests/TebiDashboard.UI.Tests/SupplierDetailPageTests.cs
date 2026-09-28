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

public class SupplierDetailPageTests
{
    [Fact]
    public void SupplierDetailPage_ShowsItemAndVendorNames_OnRelatedRows()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/suppliers/s1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(SampleDetail()));

        var cut = ctx.Render<SupplierDetailPage>(parameters => parameters.Add(p => p.SupplierId, "s1"));
        cut.WaitForElement("table[aria-label='Purchased items']", TimeSpan.FromSeconds(5));

        Assert.Contains("Betonblokken 40x20", cut.Find("table[aria-label='Purchased items']").TextContent);
        Assert.Contains("t19", cut.Find("table[aria-label='Supplier purchase orders']").TextContent);
        Assert.DoesNotContain("Betonblokken 40x20", cut.Find("table[aria-label='Supplier purchase orders']").TextContent);

        cut.Find("table[aria-label='Supplier purchase orders'] .supplier-detail__order-row").Click();
        Assert.Contains("Betonblokken 40x20", cut.Find("table[aria-label='Supplier purchase orders']").TextContent);

        var expenses = cut.Find("table[aria-label='Supplier expenses']").TextContent;
        Assert.Contains("Transport", expenses);
        Assert.Contains("Logistics Partner BV", expenses);
        Assert.Contains("Vendor", expenses);
        Assert.Contains(",00", expenses);
    }

    [Fact]
    public void SupplierDetailPage_GroupsPurchaseOrderLines_UnderParentOrder()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        var detail = SampleDetail();
        detail = new SupplierDetailDto
        {
            Supplier = detail.Supplier,
            PeriodFrom = detail.PeriodFrom,
            PeriodTo = detail.PeriodTo,
            TotalPurchased = 500m,
            PurchaseOrders =
            [
                new OpenTransactionDto
                {
                    TransactionId = "2136801-1",
                    Type = "Purchase Order",
                    ItemId = "i4",
                    ItemName = "Betonblokken 40x20",
                    SupplierId = "s1",
                    TransactionDate = new DateOnly(2026, 9, 22),
                    QuantityOpen = 12,
                    Amount = 100m,
                    Status = "Pending Receipt"
                },
                new OpenTransactionDto
                {
                    TransactionId = "2136801-2",
                    Type = "Purchase Order",
                    ItemId = "i2",
                    ItemName = "Klinkers Rood-Bruin",
                    SupplierId = "s1",
                    TransactionDate = new DateOnly(2026, 9, 22),
                    QuantityOpen = 18,
                    Amount = 200m,
                    Status = "Pending Receipt"
                },
                new OpenTransactionDto
                {
                    TransactionId = "2136801-3",
                    Type = "Purchase Order",
                    ItemId = "i3",
                    ItemName = "Tegels 60x60",
                    SupplierId = "s1",
                    TransactionDate = new DateOnly(2026, 9, 22),
                    QuantityOpen = 10,
                    Amount = 50m,
                    Status = "Pending Receipt"
                },
                new OpenTransactionDto
                {
                    TransactionId = "2136801-4",
                    Type = "Purchase Order",
                    ItemId = "i6",
                    ItemName = "Zand 0/8",
                    SupplierId = "s1",
                    TransactionDate = new DateOnly(2026, 9, 22),
                    QuantityOpen = 6,
                    Amount = 25m,
                    Status = "Pending Receipt"
                }
            ]
        };

        mockHttp.When(HttpMethod.Get, "*/api/suppliers/s1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(detail));

        var cut = ctx.Render<SupplierDetailPage>(parameters => parameters.Add(p => p.SupplierId, "s1"));
        cut.WaitForElement("table[aria-label='Supplier purchase orders']", TimeSpan.FromSeconds(5));

        var table = cut.Find("table[aria-label='Supplier purchase orders']");
        Assert.Contains("2136801", table.TextContent);
        Assert.DoesNotContain("2136801-1", table.TextContent);
        Assert.Single(cut.FindAll("table[aria-label='Supplier purchase orders'] tbody .supplier-detail__order-row"));

        cut.Find("table[aria-label='Supplier purchase orders'] .supplier-detail__order-row").Click();
        Assert.Contains("Betonblokken 40x20", cut.Find("table[aria-label='Supplier purchase orders']").TextContent);
        Assert.Contains("Zand 0/8", cut.Find("table[aria-label='Supplier purchase orders']").TextContent);
        Assert.Contains("375,00", cut.Find("table[aria-label='Supplier purchase orders'] tfoot").TextContent);
    }

    [Fact]
    public void SupplierDetailPage_ShowsOnlyTwentyMostRecentExpenseRows()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        var detail = SampleDetail();
        var expenses = Enumerable.Range(1, 25)
            .Select(index => new ExpenseDto
            {
                Id = $"e{index}",
                Date = new DateOnly(2026, 9, 24).AddDays(-index),
                Category = $"Cat {index}",
                Amount = index,
                VendorName = "Logistics Partner BV",
                SupplierId = "s1"
            })
            .ToList();

        detail = new SupplierDetailDto
        {
            Supplier = detail.Supplier,
            PeriodFrom = detail.PeriodFrom,
            PeriodTo = detail.PeriodTo,
            TotalPurchased = expenses.Sum(row => row.Amount),
            Expenses = expenses
        };

        mockHttp.When(HttpMethod.Get, "*/api/suppliers/s1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(detail));

        var cut = ctx.Render<SupplierDetailPage>(parameters => parameters.Add(p => p.SupplierId, "s1"));
        cut.WaitForElement("table[aria-label='Supplier expenses']", TimeSpan.FromSeconds(5));

        Assert.Contains("20 of 25", cut.Markup);
        Assert.Contains("Showing the 20 most recent of 25", cut.Markup);
        Assert.Contains("Cat 1", cut.Markup);
        Assert.DoesNotContain("Cat 21", cut.Markup);
        Assert.Equal(20, cut.FindAll("table[aria-label='Supplier expenses'] tbody tr").Count);
    }

    [Fact]
    public void SupplierDetailPage_FallsBackToSupplierNameOrId_WhenVendorIsBlank()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        var detail = SampleDetail();
        detail = new SupplierDetailDto
        {
            Supplier = detail.Supplier,
            Expenses =
            [
                new ExpenseDto
                {
                    Id = "e-blank-vendor",
                    Date = new DateOnly(2026, 9, 2),
                    Category = "Overig",
                    Amount = 10m,
                    VendorName = " ",
                    SupplierId = "s1",
                    SupplierName = "BetonCentrale Midden-NL"
                },
                new ExpenseDto
                {
                    Id = "e-id-only",
                    Date = new DateOnly(2026, 9, 3),
                    Category = "Overig",
                    Amount = 20m,
                    VendorName = "",
                    SupplierId = "s-missing",
                    SupplierName = ""
                }
            ],
            PurchaseOrders =
            [
                new OpenTransactionDto
                {
                    TransactionId = "t-blank",
                    Type = "Purchase Order",
                    ItemId = "i-missing",
                    ItemName = "",
                    SupplierId = "s1",
                    TransactionDate = new DateOnly(2026, 9, 2),
                    QuantityOpen = 1,
                    Amount = 5m,
                    Status = "Pending Receipt"
                }
            ],
            PurchasedItems =
            [
                new SupplierPurchasedItemDto
                {
                    ItemId = "i-missing",
                    ItemName = "",
                    TotalQuantityOpen = 1
                }
            ]
        };

        mockHttp.When(HttpMethod.Get, "*/api/suppliers/s1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(detail));

        var cut = ctx.Render<SupplierDetailPage>(parameters => parameters.Add(p => p.SupplierId, "s1"));
        cut.WaitForElement("table[aria-label='Supplier expenses']", TimeSpan.FromSeconds(5));

        var expenses = cut.Find("table[aria-label='Supplier expenses']").TextContent;
        Assert.Contains("BetonCentrale Midden-NL", expenses);
        Assert.Contains("s-missing", expenses);
        Assert.Contains("i-missing", cut.Find("table[aria-label='Purchased items']").TextContent);
        cut.Find("table[aria-label='Supplier purchase orders'] .supplier-detail__order-row").Click();
        Assert.Contains("i-missing", cut.Find("table[aria-label='Supplier purchase orders']").TextContent);
    }

    private static SupplierDetailDto SampleDetail() => new()
    {
        Supplier = new SupplierDto
        {
            SupplierId = "s1",
            Name = "BetonCentrale Midden-NL",
            City = "Nieuwegein",
            ContactEmail = "inkoop@betoncentrale.nl"
        },
        PeriodFrom = new DateOnly(2026, 9, 1),
        PeriodTo = new DateOnly(2026, 9, 24),
        TotalPurchased = 19200m,
        PurchasedItems =
        [
            new SupplierPurchasedItemDto
            {
                ItemId = "i4",
                ItemName = "Betonblokken 40x20",
                TotalQuantityOpen = 45
            }
        ],
        PurchaseOrders =
        [
            new OpenTransactionDto
            {
                TransactionId = "t19",
                Type = "Purchase Order",
                ItemId = "i4",
                ItemName = "Betonblokken 40x20",
                SupplierId = "s1",
                SupplierName = "BetonCentrale Midden-NL",
                TransactionDate = new DateOnly(2026, 9, 22),
                QuantityOpen = 45,
                Amount = 10800m,
                Status = "Pending Receipt"
            }
        ],
        Expenses =
        [
            new ExpenseDto
            {
                Id = "e9",
                Date = new DateOnly(2026, 9, 22),
                Category = "Transport",
                Amount = 8400m,
                VendorName = "Logistics Partner BV",
                SupplierId = "s3",
                SupplierName = "Logistics Partner BV"
            }
        ]
    };

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
