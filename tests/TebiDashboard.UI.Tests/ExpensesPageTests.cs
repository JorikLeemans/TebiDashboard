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

public class ExpensesPageTests
{
    [Fact]
    public void ExpensesPage_ShowsCategoryTotals_AndFiltersSupplierBills()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/expenses*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create<IReadOnlyList<ExpenseDto>>(
            [
                new ExpenseDto
                {
                    Id = "bill-1",
                    Date = new DateOnly(2026, 9, 29),
                    Category = "Transport",
                    Amount = 8_400m,
                    SupplierId = "s3",
                    SupplierName = "Logistics Partner BV"
                },
                new ExpenseDto
                {
                    Id = "bill-2",
                    Date = new DateOnly(2026, 9, 30),
                    Category = "Utilities",
                    Amount = 5_200m,
                    SupplierId = "s4",
                    SupplierName = "Energiebedrijf NL"
                }
            ]));

        var cut = ctx.Render<ExpensesPage>();
        cut.WaitForElement("table[aria-label='Expenses by supplier']", TimeSpan.FromSeconds(5));

        Assert.Contains("Expenses in period", cut.Markup);
        Assert.Contains("13.600", cut.Markup);
        Assert.Contains("Transport", cut.Markup);
        Assert.Contains("Utilities", cut.Markup);
        Assert.Contains("Logistics Partner BV", cut.Markup);
        Assert.Contains("/suppliers/s3", cut.Markup);
        Assert.Contains("8.400,00", cut.Markup);
        Assert.DoesNotContain("bill-1", cut.Markup);

        var logistics = cut.FindAll("table[aria-label='Expenses by supplier'] tbody .expenses-page__group-row")
            .Single(row => row.TextContent.Contains("Logistics Partner BV", StringComparison.Ordinal));
        logistics.Click();
        Assert.Contains("bill-1", cut.Markup);

        cut.Find("select").Change("Utilities");
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Energiebedrijf NL", cut.Markup);
            Assert.DoesNotContain("Logistics Partner BV", cut.Markup);
            Assert.Contains("5.200,00", cut.Markup);
        });
    }

    [Fact]
    public void ExpensesPage_ShowsEmptyState_WhenPeriodHasNoBills()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/expenses*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create<IReadOnlyList<ExpenseDto>>([]));

        var cut = ctx.Render<ExpensesPage>();
        cut.WaitForAssertion(() => Assert.Contains("No expenses in this period.", cut.Markup));
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

        public bool CanManageGoals() => isAuthenticated && Role == Roles.Management;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
