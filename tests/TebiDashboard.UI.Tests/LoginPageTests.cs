using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Pages;
using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

public class LoginPageTests
{
    [Fact]
    public void Login_ShowsWelcomeText()
    {
        using var ctx = CreateContext(out _);

        var cut = ctx.Render<Login>();

        Assert.Contains("Welcome back", cut.Markup);
        Assert.Contains("Sign in to your TEBI Dashboard", cut.Markup);
    }

    [Fact]
    public void Login_ShowsTebiLogo()
    {
        using var ctx = CreateContext(out _);

        var cut = ctx.Render<Login>();

        Assert.Contains("_content/TebiDashboard.UI/images/tebi-logo.png", cut.Markup);
    }

    [Fact]
    public void Login_ShowsValidation_WhenSubmittedEmpty()
    {
        using var ctx = CreateContext(out _);

        var cut = ctx.Render<Login>();
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(
                cut.Markup.Contains("Username is required") ||
                cut.Markup.Contains("Password is required"));
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Login_ShowsError_WhenCredentialsInvalid()
    {
        using var ctx = CreateContext(out var mockHttp);
        mockHttp.When(HttpMethod.Post, "*/api/auth/login*")
            .Respond(HttpStatusCode.Unauthorized);

        var cut = ctx.Render<Login>();
        cut.Find("#username").Change("wrong");
        cut.Find("#password").Change("wrong");
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(
            () => cut.Markup.Contains("Invalid username or password"),
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
        public bool IsAuthenticated { get; private set; }

        public string? Token { get; private set; }

        public string? Username { get; private set; }

        public string? Role { get; private set; }

        public event Action? OnChange;

        public bool CanAccessAdminDashboard() => false;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response)
        {
            IsAuthenticated = true;
            Token = response.Token;
            Username = response.Username;
            Role = response.Role;
            return Task.CompletedTask;
        }

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
