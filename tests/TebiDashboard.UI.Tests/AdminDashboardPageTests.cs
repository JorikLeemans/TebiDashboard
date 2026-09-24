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

public class AdminDashboardPageTests
{
    [Fact]
    public void AdminDashboard_ShowsAccessDenied_ForNonAdmin()
    {
        using var ctx = CreateContext(isAdmin: false, out _);

        var cut = ctx.Render<AdminDashboard>();

        cut.WaitForAssertion(
            () => Assert.Contains("You do not have permission", cut.Markup),
            TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void AdminDashboard_ShowsUsers_ForAdmin()
    {
        using var ctx = CreateContext(isAdmin: true, out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/admin/users*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create<IReadOnlyList<UserAccountDto>>(
            [
                new UserAccountDto { Email = "admin@tebi.nl", Role = Roles.Admin },
                new UserAccountDto { Email = "sales@tebi.nl", Role = Roles.Sales }
            ]));

        var cut = ctx.Render<AdminDashboard>();
        cut.WaitForAssertion(() => Assert.Contains("admin@tebi.nl", cut.Markup), TimeSpan.FromSeconds(5));

        Assert.Contains("Administrator dashboard", cut.Markup);
        Assert.Contains("NetSuite data sync", cut.Markup);
        Assert.Contains("sales@tebi.nl", cut.Markup);
        Assert.Contains("Not configured", cut.Markup);
        Assert.Contains("No sync has run", cut.Markup);
        Assert.True(FindNetSuiteButton(cut, "Test connection").HasAttribute("disabled"));
        Assert.True(FindNetSuiteButton(cut, "Sync NetSuite data").HasAttribute("disabled"));
    }

    [Fact]
    public void AdminDashboard_EnablesNetSuiteButtons_WhenConfigured()
    {
        using var ctx = CreateContext(isAdmin: true, out var mockHttp, CreateConfiguredSetup());
        mockHttp.When(HttpMethod.Get, "*/api/admin/users*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create<IReadOnlyList<UserAccountDto>>(
            [
                new UserAccountDto { Email = "admin@tebi.nl", Role = Roles.Admin }
            ]));

        var cut = ctx.Render<AdminDashboard>();
        cut.WaitForAssertion(
            () => Assert.Contains("credentials are configured", cut.Markup, StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(5));

        Assert.DoesNotContain("Not configured", cut.Markup);
        Assert.Contains("Last sync: succeeded", cut.Markup);
        Assert.False(FindNetSuiteButton(cut, "Test connection").HasAttribute("disabled"));
        Assert.False(FindNetSuiteButton(cut, "Sync NetSuite data").HasAttribute("disabled"));
    }

    private static BunitContext CreateContext(
        bool isAdmin,
        out MockHttpMessageHandler mockHttp,
        NetSuiteSetupStatusDto? setup = null)
    {
        var ctx = new BunitContext();
        mockHttp = new MockHttpMessageHandler();

        // Admin page always loads setup on open; default to the unconfigured API state.
        mockHttp.When(HttpMethod.Get, "*/api/netsuite/setup*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(setup ?? CreateUnconfiguredSetup()));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton(new AdminApiClient(client, new FakeAuthStateService(isAdmin)));
        ctx.Services.AddSingleton<IAuthStateService>(new FakeAuthStateService(isAdmin));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx;
    }

    private static NetSuiteSetupStatusDto CreateUnconfiguredSetup() =>
        new()
        {
            IsConfigured = false,
            CanTestConnection = false,
            CanSync = false,
            MissingSettings = ["Enabled"],
            ConfigurationMessage = "Missing NetSuite settings: Enabled.",
            DataSourceMessage = "NetSuite is not configured, so dashboard pages show locally seeded demo data instead of a live import.",
            LastSync = new NetSuiteSyncStatusDto
            {
                State = "Idle",
                Message = "No sync has run since this API process started."
            }
        };

    private static NetSuiteSetupStatusDto CreateConfiguredSetup() =>
        new()
        {
            IsConfigured = true,
            CanTestConnection = true,
            CanSync = true,
            ConfigurationMessage = "NetSuite OAuth credentials are configured on the API.",
            DataSourceMessage = "Dashboard pages show the last successful import in the local database.",
            LastSync = new NetSuiteSyncStatusDto
            {
                State = "Succeeded",
                Message = "Imported NetSuite records.",
                CompletedAt = DateTimeOffset.Parse("2026-09-24T10:00:00Z"),
                Result = new NetSuiteSyncResultDto { Success = true, Message = "Imported NetSuite records." }
            }
        };

    private static AngleSharp.Dom.IElement FindNetSuiteButton(IRenderedComponent<AdminDashboard> cut, string label) =>
        cut.FindAll("button").First(button => button.TextContent.Contains(label, StringComparison.Ordinal));

    private sealed class FakeAuthStateService(bool isAdmin) : IAuthStateService
    {
        public bool IsAuthenticated => true;

        public string? Token => "test-token";

        public string? Username => isAdmin ? "admin@tebi.nl" : "user@tebi.nl";

        public string? Role => isAdmin ? Roles.Admin : Roles.Sales;

        public event Action? OnChange;

        public bool CanAccessAdminDashboard() => isAdmin;

        public bool CanManageGoals() => isAdmin;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
