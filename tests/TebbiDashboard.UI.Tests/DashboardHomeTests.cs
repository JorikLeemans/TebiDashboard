using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using TebbiDashboard.Shared.Dtos;
using TebbiDashboard.UI.Pages;
using TebbiDashboard.UI.Services;

namespace TebbiDashboard.UI.Tests;

public class DashboardHomeTests
{
    [Fact]
    public void DashboardHome_ShowsTitle()
    {
        using var ctx = new BunitContext();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "*/api/dashboard/summary*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new DashboardSummaryDto
            {
                Revenue = 1000m,
                SalesVolume = 50m,
                GoalPercentage = 80m,
                RevenueTrend = "up",
                LowStockAlertCount = 1,
                PeriodFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                PeriodTo = DateOnly.FromDateTime(DateTime.UtcNow)
            }));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton(new DashboardApiClient(client));

        var cut = ctx.Render<DashboardHome>();

        Assert.Contains("TEBI Dashboard", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsKpiCards_WhenApiReturnsData()
    {
        using var ctx = new BunitContext();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "*/api/dashboard/summary*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new DashboardSummaryDto
            {
                Revenue = 1250000m,
                SalesVolume = 8420m,
                GoalPercentage = 87.5m,
                RevenueTrend = "up",
                LowStockAlertCount = 3,
                PeriodFrom = new DateOnly(2026, 1, 1),
                PeriodTo = new DateOnly(2026, 6, 30)
            }));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton(new DashboardApiClient(client));

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".kpi-card", TimeSpan.FromSeconds(5));

        Assert.Contains("Revenue", cut.Markup);
        Assert.Contains("Sales volume", cut.Markup);
        Assert.Contains("Goal progress", cut.Markup);
        Assert.Contains("Low stock alerts", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsError_WhenApiFails()
    {
        using var ctx = new BunitContext();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/dashboard/summary")
            .Respond(HttpStatusCode.InternalServerError);

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton(new DashboardApiClient(client));

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForAssertion(() => cut.Markup.Contains("Failed to load dashboard"), TimeSpan.FromSeconds(3));

        Assert.Contains("Failed to load dashboard", cut.Markup);
    }
}
