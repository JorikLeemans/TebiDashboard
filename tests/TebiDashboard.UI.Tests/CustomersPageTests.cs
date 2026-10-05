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
        Assert.Contains("Aannemer", cut.Markup);
        Assert.Contains("Woningbouw", cut.Markup);
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

    [Fact]
    public void CustomersPage_FiltersRows_BySearchText()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupCustomersOverview(mockHttp, CreateSampleCustomers());

        var cut = ctx.Render<CustomersPage>();
        cut.WaitForElement(".customers-table", TimeSpan.FromSeconds(5));

        cut.Find("input[type=search]").Input("gemeente");

        Assert.Contains("Gemeente Utrecht", cut.Markup);
        Assert.DoesNotContain("Bouwbedrijf Jansen", cut.Markup);
    }

    [Fact]
    public void CustomersPage_PaginatesRows_WhenPageSizeIsReduced()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        var customers = Enumerable.Range(1, 12)
            .Select(i => new CustomerOverviewDto
            {
                CustomerId = $"c{i}",
                Name = $"Customer {i}",
                City = "Utrecht",
                TotalSpent = i * 1000m,
                RevenueEntryCount = 1,
                SalesOrderCount = 0
            })
            .ToList();

        SetupCustomersOverview(mockHttp, customers);

        var cut = ctx.Render<CustomersPage>();
        cut.WaitForElement(".list-pagination", TimeSpan.FromSeconds(5));

        cut.Find("select").Change("5");

        // The page size change re-renders the table. Wait so a busy test run does not count the old rows.
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".customers-table tbody tr");
            Assert.Equal(5, rows.Count);
            Assert.Contains("Results per page", cut.Markup);
            Assert.Contains("Customer 1", cut.Markup);
            Assert.DoesNotContain("Customer 6", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CustomersPage_SortsByName_WhenHeaderIsClickedTwice()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupCustomersOverview(mockHttp,
        [
            new CustomerOverviewDto { CustomerId = "c1", Name = "Zebra BV", City = "Utrecht", TotalSpent = 10m },
            new CustomerOverviewDto { CustomerId = "c2", Name = "Alpha BV", City = "Utrecht", TotalSpent = 20m }
        ]);

        var cut = ctx.Render<CustomersPage>();
        cut.WaitForElement(".customers-table", TimeSpan.FromSeconds(5));

        var header = cut.Find("th[aria-sort]");
        header.Click();

        var ascending = cut.FindAll(".customers-table tbody tr").Select(row => row.Children[0].TextContent.Trim()).ToList();
        Assert.Equal(["Alpha BV", "Zebra BV"], ascending);
        Assert.Equal("ascending", cut.Find("th[aria-sort='ascending']").GetAttribute("aria-sort"));

        header.Click();

        var descending = cut.FindAll(".customers-table tbody tr").Select(row => row.Children[0].TextContent.Trim()).ToList();
        Assert.Equal(["Zebra BV", "Alpha BV"], descending);
        Assert.Equal("descending", cut.Find("th[aria-sort='descending']").GetAttribute("aria-sort"));
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
        var auth = new FakeAuthStateService(isAuthenticated: true);
        ctx.Services.AddSingleton(new DashboardApiClient(client, auth));
        ctx.Services.AddSingleton<IAuthStateService>(auth);
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
            Category = "Aannemer",
            Subcategory = "Woningbouw",
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
            Category = "Overheid",
            Subcategory = "Gemeente",
            ContactEmail = "aanbesteding@utrecht.nl",
            TotalSpent = 38500m,
            RevenueEntryCount = 1,
            SalesOrderCount = 0
        }
    ];

    private sealed class FakeAuthStateService(bool isAuthenticated) : IAuthStateService
    {
        // Read the constructor argument directly so it is stored once.
        public bool IsAuthenticated => isAuthenticated;

        public string? Token => isAuthenticated ? "test-token" : null;

        public string? Username => isAuthenticated ? "demo" : null;

        public string? Role => isAuthenticated ? "Management" : null;

        // The interface requires this event. These tests never subscribe, so there is no backing field.
        public event Action? OnChange { add { } remove { } }

        public bool CanAccessAdminDashboard() => isAuthenticated && Role == Roles.Admin;

        public bool CanManageGoals() => isAuthenticated && Role == Roles.Management;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
