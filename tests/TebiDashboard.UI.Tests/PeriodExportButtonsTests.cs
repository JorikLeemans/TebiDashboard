using System.Net;
using System.Net.Http.Headers;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Components;
using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

/// <summary>
/// Export buttons follow the signed-in role and save the file the API returns.
/// </summary>
public class PeriodExportButtonsTests
{
    [Fact]
    public void Buttons_AreHidden_ForReadOnly()
    {
        using var ctx = CreateContext(Roles.ReadOnly, out _);

        var cut = RenderButtons(ctx);

        Assert.DoesNotContain("Excel", cut.Markup);
        Assert.DoesNotContain("PDF", cut.Markup);
    }

    [Fact]
    public void Buttons_AreShown_ForManagement()
    {
        using var ctx = CreateContext(Roles.Management, out _);

        var cut = RenderButtons(ctx);

        Assert.Contains("Excel", cut.Markup);
        Assert.Contains("PDF", cut.Markup);
    }

    [Fact]
    public void Excel_SavesTheFileReturnedByTheApi()
    {
        using var ctx = CreateContext(Roles.Management, out var mockHttp);
        mockHttp.When(HttpMethod.Get, "https://localhost/api/exports/period?format=xlsx&from=2026-09-01&to=2026-09-30")
            .Respond(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([1, 2, 3])
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "tebi-2026-09-01-2026-09-30.xlsx"
                };
                return response;
            });
        ctx.JSInterop.SetupVoid("tebiDownloadFile", _ => true);

        var cut = RenderButtons(ctx);
        cut.FindAll("button")[0].Click();

        cut.WaitForAssertion(() =>
        {
            var call = ctx.JSInterop.VerifyInvoke("tebiDownloadFile");
            Assert.Equal("tebi-2026-09-01-2026-09-30.xlsx", call.Arguments[0]);
        });
    }

    [Fact]
    public void Excel_ShowsTheForbiddenMessage()
    {
        using var ctx = CreateContext(Roles.Management, out var mockHttp);
        mockHttp.When(HttpMethod.Get, "https://localhost/api/exports/period*")
            .Respond(HttpStatusCode.Forbidden);

        var cut = RenderButtons(ctx);
        cut.FindAll("button")[0].Click();

        cut.WaitForAssertion(() => Assert.Contains("You cannot export data.", cut.Markup));
    }

    private static IRenderedComponent<PeriodExportButtons> RenderButtons(BunitContext ctx) =>
        ctx.Render<PeriodExportButtons>(parameters => parameters
            .Add(component => component.From, new DateOnly(2026, 9, 1))
            .Add(component => component.To, new DateOnly(2026, 9, 30)));

    private static BunitContext CreateContext(string role, out MockHttpMessageHandler mockHttp)
    {
        var ctx = new BunitContext();
        mockHttp = new MockHttpMessageHandler();
        var auth = new ExportAuth(role);
        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost/");
        ctx.Services.AddSingleton<IAuthStateService>(auth);
        ctx.Services.AddSingleton(new DashboardApiClient(client, auth));
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        return ctx;
    }

    private sealed class ExportAuth(string role) : IAuthStateService
    {
        public bool IsAuthenticated => true;

        public string? Token => "token";

        public string? Username => "user";

        public string? Role => role;

        public event Action? OnChange;

        public bool CanAccessAdminDashboard() => false;

        public bool CanManageGoals() => false;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
