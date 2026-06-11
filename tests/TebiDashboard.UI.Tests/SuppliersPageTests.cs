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

public class SuppliersPageTests
{
    [Fact]
    public void SuppliersPage_ShowsTitle_WhenAuthenticated()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupSuppliersOverview(mockHttp, CreateSampleSuppliers());

        var cut = ctx.Render<SuppliersPage>();
        cut.WaitForElement(".suppliers-table", TimeSpan.FromSeconds(5));

        Assert.Contains("Suppliers", cut.Markup);
        Assert.Contains("BetonCentrale Midden-NL", cut.Markup);
    }

    [Fact]
    public void SuppliersPage_ShowsEuroAmounts_WhenApiReturnsData()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupSuppliersOverview(mockHttp,
        [
            new SupplierOverviewDto
            {
                SupplierId = "s1",
                Name = "BetonCentrale Midden-NL",
                City = "Nieuwegein",
                TotalPurchased = 22300m,
                ExpenseEntryCount = 0,
                PurchaseOrderCount = 2,
                PurchasedItemCount = 2
            }
        ]);

        var cut = ctx.Render<SuppliersPage>();
        cut.WaitForElement(".suppliers-table", TimeSpan.FromSeconds(5));

        Assert.Contains("€", cut.Markup);
        Assert.Contains("Total purchased", cut.Markup);
        Assert.Contains("Purchase orders", cut.Markup);
    }

    private static void SetupSuppliersOverview(MockHttpMessageHandler mockHttp, IReadOnlyList<SupplierOverviewDto> suppliers)
    {
        mockHttp.When(HttpMethod.Get, "*/api/suppliers/overview*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(suppliers));
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

    private static IReadOnlyList<SupplierOverviewDto> CreateSampleSuppliers() =>
    [
        new()
        {
            SupplierId = "s1",
            Name = "BetonCentrale Midden-NL",
            City = "Nieuwegein",
            ContactEmail = "inkoop@betoncentrale.nl",
            TotalPurchased = 0m,
            ExpenseEntryCount = 0,
            PurchaseOrderCount = 2,
            PurchasedItemCount = 2
        },
        new()
        {
            SupplierId = "s3",
            Name = "Logistics Partner BV",
            City = "Utrecht",
            ContactEmail = "transport@logistics.nl",
            TotalPurchased = 22300m,
            ExpenseEntryCount = 2,
            PurchaseOrderCount = 0,
            PurchasedItemCount = 0
        }
    ];

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
