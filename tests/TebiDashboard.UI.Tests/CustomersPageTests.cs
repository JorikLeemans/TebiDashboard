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

public class CustomersPageTests
{
    [Fact]
    public void CustomersPage_ShowsTitle_WhenAuthenticated()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupCustomersOverview(mockHttp, CreateSampleCustomers());

        var cut = ctx.Render<CustomersPage>();
        cut.WaitForElement(".customers-table", TimeSpan.FromSeconds(5));

        Assert.Contains("Customers", cut.Markup);
        Assert.Contains("Bouwbedrijf Jansen", cut.Markup);
    }

    [Fact]
    public void CustomersPage_ShowsEuroAmounts_WhenApiReturnsData()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupCustomersOverview(mockHttp,
        [
            new CustomerOverviewDto
            {
                CustomerId = "c1",
                Name = "Bouwbedrijf Jansen",
                City = "Utrecht",
                TotalSpent = 42000m,
                RevenueEntryCount = 2,
                SalesOrderCount = 1
            }
        ]);

        var cut = ctx.Render<CustomersPage>();
        cut.WaitForElement(".customers-table", TimeSpan.FromSeconds(5));

        Assert.Contains("€", cut.Markup);
        Assert.Contains("Total spent", cut.Markup);
    }

    private static void SetupCustomersOverview(MockHttpMessageHandler mockHttp, IReadOnlyList<CustomerOverviewDto> customers)
    {
        mockHttp.When(HttpMethod.Get, "*/api/customers/overview*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(customers));
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

    private static IReadOnlyList<CustomerOverviewDto> CreateSampleCustomers() =>
    [
        new()
        {
            CustomerId = "c1",
            Name = "Bouwbedrijf Jansen",
            City = "Utrecht",
            ContactEmail = "inkoop@jansen.nl",
            TotalSpent = 81500m,
            RevenueEntryCount = 2,
            SalesOrderCount = 1
        },
        new()
        {
            CustomerId = "c2",
            Name = "Gemeente Utrecht",
            City = "Utrecht",
            ContactEmail = "aanbesteding@utrecht.nl",
            TotalSpent = 38500m,
            RevenueEntryCount = 1,
            SalesOrderCount = 0
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
