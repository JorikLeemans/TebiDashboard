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

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".kpi-card", TimeSpan.FromSeconds(5));

        Assert.Contains("Revenue", cut.Markup);
        Assert.Contains("Sales volume", cut.Markup);
        Assert.Contains("Sales in period", cut.Markup);
        Assert.Contains("Transactions in period", cut.Markup);
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

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
