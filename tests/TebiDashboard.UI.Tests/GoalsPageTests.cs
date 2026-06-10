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

public class GoalsPageTests
{
    [Fact]
    public void GoalsPage_ShowsCompanyGoals_WhenApiReturnsData()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupGoals(mockHttp,
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
            },
            new GoalDto
            {
                Id = "g-i1",
                Year = 2026,
                Period = "Q2",
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                TargetValue = 1200m,
                ActualValue = 980m,
                Percentage = 81.7m,
                Status = "at-risk"
            }
        ]);

        var cut = ctx.Render<GoalsPage>();
        cut.WaitForElement(".goals-page__section", TimeSpan.FromSeconds(5));

        Assert.Contains("Company revenue goals", cut.Markup);
        Assert.Contains("Item goals", cut.Markup);
        Assert.Contains("Betonstraatstenen Antraciet", cut.Markup);
    }

    private static void SetupGoals(MockHttpMessageHandler mockHttp, IReadOnlyList<GoalDto> goals)
    {
        mockHttp.When(HttpMethod.Get, "*/api/goals*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(goals));
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
