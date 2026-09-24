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
        Assert.Contains("Betonblokken 40x20", cut.Find("table[aria-label='Supplier purchase orders']").TextContent);
        Assert.Contains("t19", cut.Find("table[aria-label='Supplier purchase orders']").TextContent);

        var expenses = cut.Find("table[aria-label='Supplier expenses']").TextContent;
        Assert.Contains("Transport", expenses);
        Assert.Contains("Logistics Partner BV", expenses);
        Assert.Contains("Vendor", expenses);
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
