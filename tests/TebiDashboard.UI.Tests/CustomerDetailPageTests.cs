using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Pages;
using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

/// <summary>
/// Customer detail page: invoice groups stay collapsed until the header row is opened.
/// </summary>
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
                    ContactEmail = "inkoop@jansen.nl",
                    Category = "Aannemer",
                    Subcategory = "Woningbouw"
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

        Assert.Contains("Aannemer", cut.Markup);
        Assert.Contains("Woningbouw", cut.Markup);

        // Invoice rows stay collapsed until clicked, so only the header fields are visible first.
        var revenue = cut.Find("table[aria-label='Customer revenue']").TextContent;
        Assert.Contains("F-C1", revenue);
        Assert.Contains(",50", revenue);
        Assert.Contains("Invoice", revenue);
        Assert.DoesNotContain("Opsluitbanden Grijs", revenue);

        cut.Find(".customer-detail__invoice-row").Click();
        var expandedRevenue = cut.Find("table[aria-label='Customer revenue']").TextContent;
        Assert.Contains("Opsluitbanden Grijs", expandedRevenue);
        Assert.Contains("pg-edging", expandedRevenue);

        var orders = cut.Find("table[aria-label='Customer sales orders']").TextContent;
        Assert.Contains("t8", orders);
        Assert.DoesNotContain("Opsluitbanden Grijs", orders);

        cut.Find("table[aria-label='Customer sales orders'] .customer-detail__invoice-row").Click();
        var expandedOrders = cut.Find("table[aria-label='Customer sales orders']").TextContent;
        Assert.Contains("Opsluitbanden Grijs", expandedOrders);
        var status = cut.Find("table[aria-label='Customer sales orders'] .transaction-status");
        Assert.Equal("B", status.TextContent);
        Assert.Equal("Pending Fulfillment", status.GetAttribute("title"));
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

        cut.Find("table[aria-label='Customer revenue'] tbody .customer-detail__invoice-row").Click();
        Assert.Contains("pg-unknown", cut.Find("table[aria-label='Customer revenue']").TextContent);
        cut.Find("table[aria-label='Customer sales orders'] tbody .customer-detail__invoice-row").Click();
        Assert.Contains("i-missing", cut.Find("table[aria-label='Customer sales orders']").TextContent);
    }

    [Fact]
    public void CustomerDetailPage_GroupsRevenueByInvoice_AndExpandsLinesOnClick()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/customers/c2*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new CustomerDetailDto
            {
                Customer = new CustomerDto { CustomerId = "c2", Name = "Stratenmakers BV" },
                Revenue =
                [
                    new RevenueDto
                    {
                        Id = "1842-1",
                        InvoiceId = "1842",
                        InvoiceNumber = "F-100",
                        Date = new DateOnly(2026, 9, 23),
                        Amount = 10.25m,
                        ItemName = "Klinkers",
                        ProductGroupName = "Bestrating"
                    },
                    new RevenueDto
                    {
                        Id = "1842-2",
                        InvoiceId = "1842",
                        InvoiceNumber = "F-100",
                        Date = new DateOnly(2026, 9, 23),
                        Amount = 5.50m,
                        ItemName = "Zand",
                        ProductGroupName = "Zand"
                    }
                ]
            }));

        var cut = ctx.Render<CustomerDetailPage>(parameters => parameters.Add(p => p.CustomerId, "c2"));
        cut.WaitForElement("table[aria-label='Customer revenue']", TimeSpan.FromSeconds(5));

        var collapsed = cut.Find("table[aria-label='Customer revenue']").TextContent;
        Assert.Contains("F-100", collapsed);
        Assert.Contains("15,75", collapsed);
        Assert.DoesNotContain("Klinkers", collapsed);
        Assert.DoesNotContain("Zand", collapsed);

        cut.Find("table[aria-label='Customer revenue'] tbody .customer-detail__invoice-row").Click();
        var expanded = cut.Find("table[aria-label='Customer revenue']").TextContent;
        Assert.Contains("Klinkers", expanded);
        Assert.Contains("Zand", expanded);
        Assert.Contains("Bestrating", expanded);
    }

    [Fact]
    public void CustomerDetailPage_GroupsSalesOrders_AndExpandsLinesOnClick()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/customers/c3*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new CustomerDetailDto
            {
                Customer = new CustomerDto { CustomerId = "c3", Name = "Stratenmakers BV" },
                SalesOrders =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "t1",
                        Type = "Sales Order",
                        ItemName = "Betonstraatstenen Antraciet",
                        TransactionDate = new DateOnly(2026, 9, 23),
                        QuantityOpen = 40,
                        Amount = 39500m,
                        Status = "Pending Fulfillment"
                    },
                    new OpenTransactionDto
                    {
                        TransactionId = "t1-2",
                        Type = "Sales Order",
                        ItemName = "Klinkers Rood-Bruin",
                        TransactionDate = new DateOnly(2026, 9, 23),
                        QuantityOpen = 12,
                        Amount = 14400m,
                        Status = "Pending Fulfillment"
                    }
                ]
            }));

        var cut = ctx.Render<CustomerDetailPage>(parameters => parameters.Add(p => p.CustomerId, "c3"));
        cut.WaitForElement("table[aria-label='Customer sales orders']", TimeSpan.FromSeconds(5));

        var collapsed = cut.Find("table[aria-label='Customer sales orders']").TextContent;
        Assert.Contains("t1", collapsed);
        Assert.Contains("53.900,00", collapsed);
        Assert.DoesNotContain("Betonstraatstenen Antraciet", collapsed);
        Assert.DoesNotContain("Klinkers Rood-Bruin", collapsed);

        cut.Find("table[aria-label='Customer sales orders'] .customer-detail__invoice-row").Click();
        var expanded = cut.Find("table[aria-label='Customer sales orders']").TextContent;
        Assert.Contains("Betonstraatstenen Antraciet", expanded);
        Assert.Contains("Klinkers Rood-Bruin", expanded);
    }

    [Fact]
    public void CustomerDetailPage_ExpandsSalesOrder_WhenFocusOrderQueryIsSet()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/customers/c3*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(new CustomerDetailDto
            {
                Customer = new CustomerDto { CustomerId = "c3", Name = "Bouwbedrijf Jansen" },
                SalesOrders =
                [
                    new OpenTransactionDto
                    {
                        TransactionId = "t1-1",
                        Type = "Sales Order",
                        ItemName = "Betonstraatstenen Antraciet",
                        TransactionDate = new DateOnly(2026, 9, 23),
                        QuantityOpen = 40,
                        Amount = 39500m,
                        Status = "Pending Fulfillment"
                    },
                    new OpenTransactionDto
                    {
                        TransactionId = "t1-2",
                        Type = "Sales Order",
                        ItemName = "Klinkers Rood-Bruin",
                        TransactionDate = new DateOnly(2026, 9, 23),
                        QuantityOpen = 12,
                        Amount = 14400m,
                        Status = "Pending Fulfillment"
                    }
                ]
            }));

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("order", "t1"));
        var cut = ctx.Render<CustomerDetailPage>(parameters => parameters.Add(p => p.CustomerId, "c3"));
        cut.WaitForElement("table[aria-label='Customer sales orders']", TimeSpan.FromSeconds(5));

        Assert.Contains("Betonstraatstenen Antraciet", cut.Find("table[aria-label='Customer sales orders']").TextContent);
        Assert.Contains("customer-detail__invoice-row--focus", cut.Markup);
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
