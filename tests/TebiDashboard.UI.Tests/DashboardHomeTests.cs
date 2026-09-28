using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Constants;
using TebiDashboard.UI.Pages;
using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

public class DashboardHomeTests
{
    [Fact]
    public void DashboardHome_ShowsTitle_WhenAuthenticated()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, CreateSampleOverview());
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();

        Assert.Contains("TEBI Dashboard", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsKpiCards_WhenApiReturnsData()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 6, 8),
            PeriodTo = new DateOnly(2026, 6, 10),
            Summary = new DashboardSummaryDto
            {
                Revenue = 1250000m,
                SalesVolume = 8420m,
                GoalPercentage = 87.5m,
                RevenueTrend = "up",
                LowStockAlertCount = 3,
                PeriodFrom = new DateOnly(2026, 6, 8),
                PeriodTo = new DateOnly(2026, 6, 10)
            },
            Revenue =
            [
                new RevenueDto { Id = "1", Date = new DateOnly(2026, 6, 9), Amount = 42000m, CustomerName = "Bouwbedrijf Jansen" }
            ],
            Transactions =
            [
                new OpenTransactionDto
                {
                    TransactionId = "t1",
                    Type = "Sales Order",
                    ItemId = "i1",
                    ItemName = "Betonstraatstenen",
                    TransactionDate = new DateOnly(2026, 6, 9),
                    QuantityOpen = 40,
                    Status = "Pending Fulfillment"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".kpi-card", TimeSpan.FromSeconds(5));

        Assert.Contains("Revenue", cut.Markup);
        Assert.Contains("Sales volume", cut.Markup);
        Assert.Contains("Revenue chart", cut.Markup);
        Assert.Contains("Sales in period", cut.Markup);
        Assert.Contains("Open sales orders and period transactions", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsStatusLetterWithFullNameOnHover()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 6, 8),
            PeriodTo = new DateOnly(2026, 6, 10),
            Summary = new DashboardSummaryDto
            {
                Revenue = 0m,
                SalesVolume = 0m,
                GoalPercentage = 0m,
                RevenueTrend = "flat",
                LowStockAlertCount = 0,
                PeriodFrom = new DateOnly(2026, 6, 8),
                PeriodTo = new DateOnly(2026, 6, 10)
            },
            Transactions =
            [
                new OpenTransactionDto
                {
                    TransactionId = "t-status",
                    Type = "Sales Order",
                    ItemId = "i1",
                    ItemName = "Betonstraatstenen",
                    TransactionDate = new DateOnly(2026, 6, 9),
                    QuantityOpen = 40,
                    Status = "SalesOrd:B"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement("table[aria-label='Open sales orders and period transactions']", TimeSpan.FromSeconds(5));

        var status = cut.Find(".transaction-status");
        Assert.Equal("B", status.TextContent);
        Assert.Equal("Pending Fulfillment", status.GetAttribute("title"));
        Assert.Equal("Pending Fulfillment", status.GetAttribute("aria-label"));
    }

    [Fact]
    public void DashboardHome_LinksCustomerInSalesTable_WhenCustomerIdPresent()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 6, 8),
            PeriodTo = new DateOnly(2026, 6, 10),
            Summary = new DashboardSummaryDto
            {
                Revenue = 42000m,
                SalesVolume = 1m,
                GoalPercentage = 80m,
                RevenueTrend = "up",
                LowStockAlertCount = 0,
                PeriodFrom = new DateOnly(2026, 6, 8),
                PeriodTo = new DateOnly(2026, 6, 10)
            },
            Revenue =
            [
                new RevenueDto
                {
                    Id = "1",
                    Date = new DateOnly(2026, 6, 9),
                    Amount = 42000.5m,
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    InvoiceNumber = "F-C1"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".dashboard-table__customer-link", TimeSpan.FromSeconds(5));

        Assert.Contains("href=\"/customers/c1\"", cut.Markup);
        Assert.Contains("Bouwbedrijf Jansen", cut.Markup);
        Assert.Contains("F-C1", cut.Markup);
        Assert.Contains("Invoice", cut.Markup);
        Assert.Contains(",50", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsOnlyTwentyMostRecentSalesRows()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        var revenue = Enumerable.Range(1, 25)
            .Select(index => new RevenueDto
            {
                Id = $"r{index}",
                Date = new DateOnly(2026, 6, 10).AddDays(-index),
                Amount = index,
                CustomerName = $"Customer {index}",
                InvoiceNumber = $"F-{index}"
            })
            .ToList();

        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 5, 1),
            PeriodTo = new DateOnly(2026, 6, 10),
            Summary = new DashboardSummaryDto
            {
                Revenue = 1000m,
                SalesVolume = 25m,
                GoalPercentage = 80m,
                RevenueTrend = "up",
                LowStockAlertCount = 0,
                PeriodFrom = new DateOnly(2026, 5, 1),
                PeriodTo = new DateOnly(2026, 6, 10)
            },
            Revenue = revenue
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".dashboard-table", TimeSpan.FromSeconds(5));

        Assert.Contains("20 of 25", cut.Markup);
        Assert.Contains("Showing the 20 most recent of 25", cut.Markup);
        Assert.Contains("F-1", cut.Markup);
        Assert.DoesNotContain("F-21", cut.Markup);
        Assert.Equal(20, cut.FindAll("table[aria-label=\"Revenue in period\"] tbody tr").Count);
    }

    [Fact]
    public void DashboardHome_ShowsError_WhenApiFails()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/dashboard/period-overview*")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".error", TimeSpan.FromSeconds(5));

        Assert.Contains("Failed to load dashboard", cut.Markup);
    }

    private static void SetupPeriodOverview(MockHttpMessageHandler mockHttp, PeriodOverviewDto overview)
    {
        mockHttp.When(HttpMethod.Get, "*/api/dashboard/period-overview*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(overview));
    }

    private static void SetupGoals(MockHttpMessageHandler mockHttp)
    {
        mockHttp.When(HttpMethod.Get, "*/api/goals*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create<IReadOnlyList<GoalDto>>(
            [
                new GoalDto
                {
                    Id = "g-q2",
                    Year = 2026,
                    Period = "Q2",
                    TargetValue = 500_000m,
                    ActualValue = 437_500m,
                    Percentage = 87.5m,
                    Status = "on-track"
                }
            ]));
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

    private static PeriodOverviewDto CreateSampleOverview() => new()
    {
        PeriodFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
        PeriodTo = DateOnly.FromDateTime(DateTime.UtcNow),
        Summary = new DashboardSummaryDto
        {
            Revenue = 1000m,
            SalesVolume = 50m,
            GoalPercentage = 80m,
            RevenueTrend = "up",
            LowStockAlertCount = 1,
            PeriodFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            PeriodTo = DateOnly.FromDateTime(DateTime.UtcNow)
        }
    };

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
