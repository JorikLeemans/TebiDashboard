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

public class CustomerDetailPageTests
{
    [Fact]
    public void CustomerDetailPage_ShowsItemNames_OnRevenueAndSalesOrders()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/customers/c1*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new CustomerDetailDto
            {
                Customer = new CustomerDto
                {
                    CustomerId = "c1",
                    Name = "Bouwbedrijf Jansen",
                    City = "Utrecht",
                    ContactEmail = "inkoop@jansen.nl"
                },
                PeriodFrom = new DateOnly(2026, 9, 1),
                PeriodTo = new DateOnly(2026, 9, 24),
                TotalSpent = 6100m,
                Revenue =
                [
                    new RevenueDto
                    {
                        Id = "r38",
                        Date = new DateOnly(2026, 9, 23),
                        Amount = 6100.5m,
                        CustomerId = "c1",
                        CustomerName = "Bouwbedrijf Jansen",
                        ProductGroupId = "pg-edging",
                        ItemId = "i5",
                        ItemName = "Opsluitbanden Grijs",
                        InvoiceId = "c1",
                        InvoiceNumber = "F-C1"
                    }
                ],
                SalesOrders =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "t8",
                        Type = "Sales Order",
                        ItemId = "i5",
                        ItemName = "Opsluitbanden Grijs",
                        CustomerId = "c1",
                        CustomerName = "Bouwbedrijf Jansen",
                        TransactionDate = new DateOnly(2026, 9, 13),
                        QuantityOpen = 18,
                        Amount = 27700m,
                        Status = "Pending Fulfillment"
                    }
                ]
            }));

        var cut = ctx.Render<CustomerDetailPage>(parameters => parameters.Add(p => p.CustomerId, "c1"));
        cut.WaitForElement("table[aria-label='Customer revenue']", TimeSpan.FromSeconds(5));

        var revenue = cut.Find("table[aria-label='Customer revenue']").TextContent;
        Assert.Contains("Opsluitbanden Grijs", revenue);
        Assert.Contains("pg-edging", revenue);
        Assert.Contains("F-C1", revenue);
        Assert.Contains(",50", revenue);
        Assert.Contains("Item", cut.Find("table[aria-label='Customer revenue']").TextContent);
        Assert.Contains("Invoice", cut.Find("table[aria-label='Customer revenue']").TextContent);

        var orders = cut.Find("table[aria-label='Customer sales orders']").TextContent;
        Assert.Contains("Opsluitbanden Grijs", orders);
        Assert.Contains("t8", orders);
    }

    [Fact]
    public void CustomerDetailPage_FallsBackToIds_WhenNamesAreMissing()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/customers/c9*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new CustomerDetailDto
            {
                Customer = new CustomerDto { CustomerId = "c9", Name = "Onbekend" },
                Revenue =
                [
                    new RevenueDto
                    {
                        Id = "rx",
                        Date = new DateOnly(2026, 9, 1),
                        Amount = 100m,
                        ProductGroupId = "pg-unknown"
                    }
                ],
                SalesOrders =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "tx",
                        Type = "Sales Order",
                        ItemId = "i-missing",
                        ItemName = "",
                        TransactionDate = new DateOnly(2026, 9, 1),
                        QuantityOpen = 1,
                        Amount = 50m,
                        Status = "Pending Fulfillment"
                    }
                ]
            }));

        var cut = ctx.Render<CustomerDetailPage>(parameters => parameters.Add(p => p.CustomerId, "c9"));
        cut.WaitForElement("table[aria-label='Customer revenue']", TimeSpan.FromSeconds(5));

        Assert.Contains("pg-unknown", cut.Find("table[aria-label='Customer revenue']").TextContent);
        Assert.Contains("i-missing", cut.Find("table[aria-label='Customer sales orders']").TextContent);
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

        public bool CanManageGoals() => isAuthenticated && Role == Roles.Management;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SignInAsync(LoginResponseDto response) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
