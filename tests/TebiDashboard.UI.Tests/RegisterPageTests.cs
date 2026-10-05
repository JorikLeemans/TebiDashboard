using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Pages;
using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

public class RegisterPageTests
{
    [Fact]
    public void Register_ShowsEmailAndPasswordFields()
    {
        using var ctx = CreateContext(out _);

        var cut = ctx.Render<Register>();

        Assert.Contains("Create your account", cut.Markup);
        Assert.Contains("Confirm password", cut.Markup);
        cut.Find("#email");
        cut.Find("#password");
        cut.Find("#confirm-password");
    }

    [Fact]
    public void Register_ShowsPasswordMismatchValidation()
    {
        using var ctx = CreateContext(out _);

        var cut = ctx.Render<Register>();
        cut.Find("#email").Change("user@tebi.nl");
        cut.Find("#password").Change("password123");
        cut.Find("#confirm-password").Change("different123");
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(
            () => Assert.Contains("Passwords do not match", cut.Markup),
            TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Register_ShowsError_WhenEmailAlreadyExists()
    {
        using var ctx = CreateContext(out var mockHttp);
        mockHttp.When(HttpMethod.Post, "*/api/auth/register*")
            .Respond(HttpStatusCode.Conflict, JsonContent.Create(new { message = "An account with this email already exists." }));

        var cut = ctx.Render<Register>();
        cut.Find("#email").Change("existing@tebi.nl");
        cut.Find("#password").Change("password123");
        cut.Find("#confirm-password").Change("password123");
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(
            () => Assert.Contains("already exists", cut.Markup),
            TimeSpan.FromSeconds(5));
    }

    private static BunitContext CreateContext(out MockHttpMessageHandler mockHttp)
    {
        var ctx = new BunitContext();
        mockHttp = new MockHttpMessageHandler();

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton(new AuthApiClient(client));
        ctx.Services.AddSingleton<IAuthStateService>(new FakeAuthStateService());
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx;
    }

    private sealed class FakeAuthStateService : IAuthStateService
    {
        public bool IsAuthenticated => false;

        public string? Token => null;

        public string? Username => null;

        public string? Role => null;

        // The interface requires this event. These tests never subscribe, so there is no backing field.
        public event Action? OnChange { add { } remove { } }

        public bool CanAccessAdminDashboard() => false;

        public bool CanManageGoals() => false;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
