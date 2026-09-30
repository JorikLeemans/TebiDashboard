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

public class GoalSettingsPageTests
{
    [Fact]
    public void GoalSettings_SumsQuarterTargetsIntoYearTotal()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupSettings(mockHttp,
        [
            new GoalDto
            {
                Id = "g-i1-q1",
                Year = DateTime.UtcNow.Year,
                Period = "Q1",
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                TargetValue = 100m
            },
            new GoalDto
            {
                Id = "g-i1-q3",
                Year = DateTime.UtcNow.Year,
                Period = "Q3",
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                TargetValue = 50m
            },
            new GoalDto
            {
                Id = "g-i1-year",
                Year = DateTime.UtcNow.Year,
                Period = "YEAR",
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                TargetValue = 999m
            }
        ]);

        var cut = ctx.Render<GoalSettingsPage>();
        var year = cut.WaitForElement("input[aria-label='Betonstraatstenen Antraciet Year total target']");

        Assert.Equal("150", year.GetAttribute("value"));
        Assert.True(year.HasAttribute("readonly"));
    }

    [Fact]
    public void GoalSettings_KeepsYearTotal_WhenQuartersAreEmpty()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupSettings(mockHttp,
        [
            new GoalDto
            {
                Id = "g-i1-year",
                Year = DateTime.UtcNow.Year,
                Period = "YEAR",
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                TargetValue = 800m
            }
        ]);

        var cut = ctx.Render<GoalSettingsPage>();
        var year = cut.WaitForElement("input[aria-label='Betonstraatstenen Antraciet Year total target']");

        Assert.Equal("800", year.GetAttribute("value"));
        Assert.False(year.HasAttribute("readonly"));
    }

    [Fact]
    public void GoalSettings_ReplacesTypedYearTotal_WhenAQuarterIsEntered()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupSettings(mockHttp,
        [
            new GoalDto
            {
                Id = "g-i1-year",
                Year = DateTime.UtcNow.Year,
                Period = "YEAR",
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                TargetValue = 800m
            }
        ]);

        var cut = ctx.Render<GoalSettingsPage>();
        cut.WaitForElement("input[aria-label='Betonstraatstenen Antraciet Q2 target']")
            .Change("40");

        var year = cut.Find("input[aria-label='Betonstraatstenen Antraciet Year total target']");
        Assert.Equal("40", year.GetAttribute("value"));
        Assert.True(year.HasAttribute("readonly"));
    }

    private static void SetupSettings(MockHttpMessageHandler mockHttp, IReadOnlyList<GoalDto> goals)
    {
        mockHttp.When(HttpMethod.Get, "*/api/inventory/items*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create<IReadOnlyList<InventoryItemDto>>(
            [
                new InventoryItemDto { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet" }
            ]));

        mockHttp.When(HttpMethod.Get, "*/api/goals*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(goals));
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

        public bool CanManageGoals() => isAuthenticated && RoleAuthorization.CanManageGoals(Role);

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
