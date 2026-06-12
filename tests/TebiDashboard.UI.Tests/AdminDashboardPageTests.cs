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
        Assert.Contains("sales@tebi.nl", cut.Markup);
    }

    private static BunitContext CreateContext(bool isAdmin, out MockHttpMessageHandler mockHttp)
    {
        var ctx = new BunitContext();
        mockHttp = new MockHttpMessageHandler();

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton(new AdminApiClient(client, new FakeAuthStateService(isAdmin)));
        ctx.Services.AddSingleton<IAuthStateService>(new FakeAuthStateService(isAdmin));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx;
    }

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
