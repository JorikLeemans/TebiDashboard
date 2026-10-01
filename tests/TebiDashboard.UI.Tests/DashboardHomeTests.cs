using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Constants;
using TebiDashboard.UI.Pages;
using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Tests;

public class DashboardHomeTests
{
    [Fact]
    public void DashboardHome_ShowsTitle_WhenAuthenticated()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, CreateSampleOverview());
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();

        Assert.Contains("TEBI Dashboard", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsPopularArticles_ForTheSelectedPeriod()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, CreateSampleOverview(),
        [
            new TopSellingItemDto
            {
                ItemId = "i1",
                ItemName = "Betonstraatstenen Antraciet",
                ProductGroupName = "Bestrating",
                    SalesAmount = 12_500m,
                    UnitsSold = 40m
            }
        ]);
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".popular-list", TimeSpan.FromSeconds(5));

        Assert.Contains("Popular articles", cut.Markup);
        Assert.Contains("recommend it", cut.Markup);
        Assert.Contains("Betonstraatstenen Antraciet", cut.Markup);
        Assert.Contains("12.500", cut.Markup);
        Assert.Contains("40", cut.Markup);
        Assert.Contains("/inventory/i1", cut.Markup);
        Assert.DoesNotContain("Product group", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsKpiCards_WhenApiReturnsData()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 6, 8),
            PeriodTo = new DateOnly(2026, 6, 10),
            Summary = new DashboardSummaryDto
            {
                Revenue = 1250000m,
                SalesVolume = 8420m,
                GoalPercentage = 87.5m,
                RevenueTrend = "up",
                LowStockAlertCount = 3,
                PeriodFrom = new DateOnly(2026, 6, 8),
                PeriodTo = new DateOnly(2026, 6, 10)
            },
            Revenue =
            [
                new RevenueDto { Id = "1", Date = new DateOnly(2026, 6, 9), Amount = 42000m, CustomerName = "Bouwbedrijf Jansen" }
            ],
            Transactions =
            [
                new OpenTransactionDto
                {
                    TransactionId = "t1",
                    Type = "Sales Order",
                    ItemId = "i1",
                    ItemName = "Betonstraatstenen",
                    TransactionDate = new DateOnly(2026, 6, 9),
                    QuantityOpen = 40,
                    Status = "Pending Fulfillment"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".kpi-card", TimeSpan.FromSeconds(5));

        Assert.Contains("Revenue", cut.Markup);
        Assert.Contains("Sales volume", cut.Markup);
        Assert.Contains("Revenue chart", cut.Markup);
        Assert.Contains("Sales in period", cut.Markup);
        Assert.Contains("Open sales orders and period transactions", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsStatusLetterWithFullNameOnHover()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 6, 8),
            PeriodTo = new DateOnly(2026, 6, 10),
            Summary = new DashboardSummaryDto
            {
                Revenue = 0m,
                SalesVolume = 0m,
                GoalPercentage = 0m,
                RevenueTrend = "flat",
                LowStockAlertCount = 0,
                PeriodFrom = new DateOnly(2026, 6, 8),
                PeriodTo = new DateOnly(2026, 6, 10)
            },
            Transactions =
            [
                new OpenTransactionDto
                {
                    TransactionId = "t-status",
                    Type = "Sales Order",
                    ItemId = "i1",
                    ItemName = "Betonstraatstenen",
                    TransactionDate = new DateOnly(2026, 6, 9),
                    QuantityOpen = 40,
                    Status = "SalesOrd:B"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement("table[aria-label='Open sales orders and period transactions']", TimeSpan.FromSeconds(5));

        var status = cut.Find(".transaction-status");
        Assert.Equal("B", status.TextContent);
        Assert.Equal("Pending Fulfillment", status.GetAttribute("title"));
        Assert.Equal("Pending Fulfillment", status.GetAttribute("aria-label"));
    }

    [Fact]
    public void DashboardHome_LinksCustomerInSalesTable_WhenCustomerIdPresent()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 6, 8),
            PeriodTo = new DateOnly(2026, 6, 10),
            Summary = new DashboardSummaryDto
            {
                Revenue = 42000m,
                SalesVolume = 1m,
                GoalPercentage = 80m,
                RevenueTrend = "up",
                LowStockAlertCount = 0,
                PeriodFrom = new DateOnly(2026, 6, 8),
                PeriodTo = new DateOnly(2026, 6, 10)
            },
            Revenue =
            [
                new RevenueDto
                {
                    Id = "1",
                    Date = new DateOnly(2026, 6, 9),
                    Amount = 42000.5m,
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    InvoiceNumber = "F-C1"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".dashboard-table__customer-link", TimeSpan.FromSeconds(5));

        Assert.Contains("href=\"/customers/c1\"", cut.Markup);
        Assert.Contains("Bouwbedrijf Jansen", cut.Markup);
        Assert.Contains("F-C1", cut.Markup);
        Assert.Contains("Invoice", cut.Markup);
        Assert.Contains(",50", cut.Markup);
    }

    [Fact]
    public void DashboardHome_GroupsSalesLines_UnderInvoiceId()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            Summary = new DashboardSummaryDto { RevenueTrend = "flat" },
            Revenue =
            [
                new RevenueDto
                {
                    Id = "1842-1",
                    InvoiceId = "1842",
                    InvoiceNumber = "F-100",
                    Date = new DateOnly(2026, 9, 23),
                    Amount = 10.25m,
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    ItemName = "Klinkers"
                },
                new RevenueDto
                {
                    Id = "1842-2",
                    InvoiceId = "1842",
                    InvoiceNumber = "F-100",
                    Date = new DateOnly(2026, 9, 23),
                    Amount = 5.50m,
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    ItemName = "Zand"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement("table[aria-label='Revenue in period']", TimeSpan.FromSeconds(5));

        var collapsed = cut.Find("table[aria-label='Revenue in period']").TextContent;
        Assert.Contains("F-100", collapsed);
        Assert.Contains("15,75", collapsed);
        Assert.DoesNotContain("Klinkers", collapsed);

        cut.Find("table[aria-label='Revenue in period'] tbody .dashboard-table__group-row").Click();
        Assert.Contains("Klinkers", cut.Find("table[aria-label='Revenue in period']").TextContent);
        Assert.Contains("Zand", cut.Find("table[aria-label='Revenue in period']").TextContent);
    }

    [Fact]
    public void DashboardHome_GroupsTransactions_UnderOrderId()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            Summary = new DashboardSummaryDto { RevenueTrend = "flat" },
            Transactions =
            [
                new OpenTransactionDto
                {
                    TransactionId = "2136801-1",
                    Type = "Sales Order",
                    ItemId = "i1",
                    ItemName = "Betonblokken",
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    TransactionDate = new DateOnly(2026, 9, 22),
                    QuantityOpen = 12,
                    Amount = 100m,
                    Status = "Pending Fulfillment"
                },
                new OpenTransactionDto
                {
                    TransactionId = "2136801-2",
                    Type = "Sales Order",
                    ItemId = "i2",
                    ItemName = "Zand 0/8",
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    TransactionDate = new DateOnly(2026, 9, 22),
                    QuantityOpen = 6,
                    Amount = 25m,
                    Status = "Pending Fulfillment"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement("table[aria-label='Open sales orders and period transactions']", TimeSpan.FromSeconds(5));

        var table = cut.Find("table[aria-label='Open sales orders and period transactions']");
        Assert.Contains("2136801", table.TextContent);
        Assert.DoesNotContain("2136801-1", table.TextContent);
        Assert.DoesNotContain("Betonblokken", table.TextContent);
        Assert.Single(cut.FindAll("table[aria-label='Open sales orders and period transactions'] tbody .dashboard-table__group-row"));

        cut.Find("table[aria-label='Open sales orders and period transactions'] tbody .dashboard-table__group-row").Click();
        Assert.Contains("Betonblokken", table.TextContent);
        Assert.Contains("Zand 0/8", table.TextContent);
    }

    [Fact]
    public void DashboardHome_GroupsExpenses_BySupplierId_AndDoesNotSayVendor()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            Summary = new DashboardSummaryDto { RevenueTrend = "flat" },
            Expenses =
            [
                new ExpenseDto
                {
                    Id = "bill-1",
                    Date = new DateOnly(2026, 9, 2),
                    Category = "Transport",
                    Amount = 10m,
                    SupplierId = "s1",
                    SupplierName = "BetonCentrale Midden-NL",
                    VendorName = "BetonCentrale Midden-NL"
                },
                new ExpenseDto
                {
                    Id = "bill-2",
                    Date = new DateOnly(2026, 9, 3),
                    Category = "Overig",
                    Amount = 20m,
                    SupplierId = "s1",
                    SupplierName = "BetonCentrale Midden-NL",
                    VendorName = "BetonCentrale Midden-NL"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement("table[aria-label='Expenses in period']", TimeSpan.FromSeconds(5));

        var collapsed = cut.Find("table[aria-label='Expenses in period']").TextContent;
        Assert.Contains("Supplier", collapsed);
        Assert.DoesNotContain("Vendor", collapsed);
        Assert.Contains("s1", collapsed);
        Assert.Contains("BetonCentrale Midden-NL", collapsed);
        Assert.DoesNotContain("bill-1", collapsed);
        Assert.Single(cut.FindAll("table[aria-label='Expenses in period'] tbody .dashboard-table__group-row"));

        cut.Find("table[aria-label='Expenses in period'] tbody .dashboard-table__group-row").Click();
        var expanded = cut.Find("table[aria-label='Expenses in period']").TextContent;
        Assert.Contains("bill-1", expanded);
        Assert.Contains("bill-2", expanded);
        Assert.Contains("30,00", expanded);
        Assert.Contains("View all expenses", cut.Markup);
        Assert.Contains("/expenses", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsOnlyTwentyMostRecentSalesRows()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        var revenue = Enumerable.Range(1, 25)
            .Select(index => new RevenueDto
            {
                Id = $"r{index}",
                Date = new DateOnly(2026, 6, 10).AddDays(-index),
                Amount = index,
                CustomerName = $"Customer {index}",
                InvoiceNumber = $"F-{index}"
            })
            .ToList();

        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 5, 1),
            PeriodTo = new DateOnly(2026, 6, 10),
            Summary = new DashboardSummaryDto
            {
                Revenue = 1000m,
                SalesVolume = 25m,
                GoalPercentage = 80m,
                RevenueTrend = "up",
                LowStockAlertCount = 0,
                PeriodFrom = new DateOnly(2026, 5, 1),
                PeriodTo = new DateOnly(2026, 6, 10)
            },
            Revenue = revenue
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".dashboard-table", TimeSpan.FromSeconds(5));

        Assert.Contains("20 of 25", cut.Markup);
        Assert.Contains("Showing the 20 most recent of 25", cut.Markup);
        Assert.Contains("F-1", cut.Markup);
        Assert.DoesNotContain("F-21", cut.Markup);
        Assert.Equal(20, cut.FindAll("table[aria-label=\"Revenue in period\"] tbody tr").Count);
    }

    [Fact]
    public void DashboardHome_ShowsError_WhenApiFails()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        mockHttp.When(HttpMethod.Get, "*/api/dashboard/period-overview*")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement(".error", TimeSpan.FromSeconds(5));

        Assert.Contains("Failed to load dashboard", cut.Markup);
    }

    [Fact]
    public void DashboardHome_ShowsPeriodTrend_AgainstEarlierWindows()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 9, 29),
            PeriodTo = new DateOnly(2026, 10, 1),
            Summary = new DashboardSummaryDto
            {
                Revenue = 67_300m,
                RevenueTrend = "up",
                PeriodFrom = new DateOnly(2026, 9, 29),
                PeriodTo = new DateOnly(2026, 10, 1)
            },
            Trend =
            [
                new PeriodTrendPointDto
                {
                    From = new DateOnly(2026, 9, 23),
                    To = new DateOnly(2026, 9, 25),
                    Revenue = 10_000m,
                    Expenses = 2_000m
                },
                new PeriodTrendPointDto
                {
                    From = new DateOnly(2026, 9, 26),
                    To = new DateOnly(2026, 9, 28),
                    Revenue = 50_000m,
                    Expenses = 4_000m
                },
                new PeriodTrendPointDto
                {
                    From = new DateOnly(2026, 9, 29),
                    To = new DateOnly(2026, 10, 1),
                    Revenue = 67_300m,
                    Expenses = 1_500m
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement("table[aria-label='Period trend']", TimeSpan.FromSeconds(5));

        Assert.Contains("Trend", cut.Markup);
        Assert.Contains("29-09-2026 – 01-10-2026", cut.Markup);
        Assert.Contains("26-09-2026 – 28-09-2026", cut.Markup);
        Assert.Contains("+35%", cut.Markup);
        Assert.Contains("+35% vs 26-09-2026 – 28-09-2026", cut.Markup);
    }

    [Fact]
    public void DashboardHome_SortsSales_WhenCustomerHeaderIsClicked()
    {
        using var ctx = CreateAuthenticatedContext(out var mockHttp);
        SetupPeriodOverview(mockHttp, new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 9, 29),
            PeriodTo = new DateOnly(2026, 10, 1),
            Summary = new DashboardSummaryDto
            {
                Revenue = 30m,
                PeriodFrom = new DateOnly(2026, 9, 29),
                PeriodTo = new DateOnly(2026, 10, 1)
            },
            Revenue =
            [
                new RevenueDto
                {
                    Id = "inv-z",
                    InvoiceNumber = "F-Z",
                    Date = new DateOnly(2026, 9, 30),
                    Amount = 20m,
                    CustomerId = "c-z",
                    CustomerName = "Zebra BV"
                },
                new RevenueDto
                {
                    Id = "inv-a",
                    InvoiceNumber = "F-A",
                    Date = new DateOnly(2026, 9, 29),
                    Amount = 10m,
                    CustomerId = "c-a",
                    CustomerName = "Alpha BV"
                }
            ]
        });
        SetupGoals(mockHttp);

        var cut = ctx.Render<DashboardHome>();
        cut.WaitForElement("table[aria-label='Revenue in period']", TimeSpan.FromSeconds(5));

        Assert.Equal(["c-zZebra BV", "c-aAlpha BV"], CustomerNames(cut));

        cut.FindAll("table[aria-label='Revenue in period'] th")[2].Click();

        Assert.Equal(["c-aAlpha BV", "c-zZebra BV"], CustomerNames(cut));
    }

    private static List<string> CustomerNames(IRenderedComponent<DashboardHome> cut) =>
        cut.FindAll("table[aria-label='Revenue in period'] tbody tr")
            .Select(row => row.Children[2].TextContent.Trim())
            .ToList();

    private static void SetupPeriodOverview(
        MockHttpMessageHandler mockHttp,
        PeriodOverviewDto overview,
        IReadOnlyList<TopSellingItemDto>? popular = null)
    {
        mockHttp.When(HttpMethod.Get, "*/api/dashboard/period-overview*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(overview));
        var popularItems = popular ?? [];
        mockHttp.When(HttpMethod.Get, "*/api/sales-volume/popular*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create(popularItems));
    }

    private static void SetupGoals(MockHttpMessageHandler mockHttp)
    {
        mockHttp.When(HttpMethod.Get, "*/api/goals*")
            .Respond(HttpStatusCode.OK, _ => JsonContent.Create<IReadOnlyList<GoalDto>>(
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
                }
            ]));
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

    private static PeriodOverviewDto CreateSampleOverview() => new()
    {
        PeriodFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
        PeriodTo = DateOnly.FromDateTime(DateTime.UtcNow),
        Summary = new DashboardSummaryDto
        {
            Revenue = 1000m,
            SalesVolume = 50m,
            GoalPercentage = 80m,
            RevenueTrend = "up",
            LowStockAlertCount = 1,
            PeriodFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            PeriodTo = DateOnly.FromDateTime(DateTime.UtcNow)
        }
    };

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
