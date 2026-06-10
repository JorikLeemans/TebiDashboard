using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// Mock NetSuite implementation with sample TEBI-like data for development and tests.
/// </summary>
public sealed class MockNetSuiteService : INetSuiteService
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var revenue = GetMockRevenue().Where(r => r.Date >= filter.From && r.Date <= filter.To).ToList();
        var transactions = GetMockTransactions().Where(t => t.TransactionDate >= filter.From && t.TransactionDate <= filter.To).ToList();
        var salesVolume = transactions.Sum(t => t.QuantityOpen);
        if (salesVolume == 0)
        {
            salesVolume = revenue.Count;
        }

        var summary = new DashboardSummaryDto
        {
            Revenue = revenue.Sum(r => r.Amount),
            SalesVolume = salesVolume,
            GoalPercentage = 87.5m,
            RevenueTrend = "up",
            LowStockAlertCount = 3,
            PeriodFrom = filter.From,
            PeriodTo = filter.To
        };

        return Task.FromResult(summary);
    }

    public Task<PeriodOverviewDto> GetPeriodOverviewAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var revenue = GetMockRevenue().Where(r => r.Date >= filter.From && r.Date <= filter.To).ToList();
        var expenses = GetMockExpenses().Where(e => e.Date >= filter.From && e.Date <= filter.To).ToList();
        var transactions = GetMockTransactions().Where(t => t.TransactionDate >= filter.From && t.TransactionDate <= filter.To).ToList();
        var lowStock = GetMockLowStock();
        var activeIds = transactions.Select(t => t.ItemId).ToHashSet();

        var overview = new PeriodOverviewDto
        {
            PeriodFrom = filter.From,
            PeriodTo = filter.To,
            Summary = new DashboardSummaryDto
            {
                Revenue = revenue.Sum(r => r.Amount),
                SalesVolume = transactions.Sum(t => t.QuantityOpen),
                GoalPercentage = 87.5m,
                RevenueTrend = "up",
                LowStockAlertCount = lowStock.Count,
                PeriodFrom = filter.From,
                PeriodTo = filter.To
            },
            Revenue = revenue,
            Expenses = expenses,
            Transactions = transactions,
            LowStockItems = lowStock,
            InventoryWithActivity = GetMockInventory().Where(i => activeIds.Contains(i.ItemId)).ToList()
        };

        return Task.FromResult(overview);
    }

    public Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var data = GetMockRevenue().Where(r => r.Date >= filter.From && r.Date <= filter.To).ToList();
        return Task.FromResult<IReadOnlyList<RevenueDto>>(data);
    }

    private static List<RevenueDto> GetMockRevenue() =>
    [
        new() { Id = "1", Date = Today.AddDays(-30), Amount = 42000m, CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", ProductGroupId = "pg1" },
        new() { Id = "2", Date = Today.AddDays(-15), Amount = 38500m, CustomerId = "c2", CustomerName = "Gemeente Utrecht", ProductGroupId = "pg2" },
        new() { Id = "3", Date = Today.AddDays(-7), Amount = 51200m, CustomerId = "c3", CustomerName = "Stratenmakers BV", ProductGroupId = "pg1" },
        new() { Id = "4", Date = Today.AddDays(-2), Amount = 39500m, CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", ProductGroupId = "pg1" },
        new() { Id = "5", Date = Today.AddDays(-1), Amount = 27800m, CustomerId = "c4", CustomerName = "Woningbouw De Vliet", ProductGroupId = "pg2" }
    ];

    public Task<IReadOnlyList<GoalDto>> GetGoalsAsync(int year, string? period = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GoalDto> goals =
        [
            new() { Id = "g1", Year = year, Period = period ?? "Q2", Type = "Revenue", TargetValue = 500_000m, ActualValue = 437_500m, Percentage = 0, Status = "on-track" },
            new() { Id = "g2", Year = year, Period = period ?? "Q2", Type = "Revenue", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", TargetValue = 1200m, ActualValue = 980m, Percentage = 0, Status = "on-track" }
        ];

        return Task.FromResult(goals);
    }

    public Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(bool highSalesOnly, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InventoryItemDto>>(GetMockLowStock());

    private static List<InventoryItemDto> GetMockLowStock() =>
    [
        new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 45, ReorderPoint = 100, RecentSalesVolume = 320, IsLowStock = true },
        new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 22, ReorderPoint = 80, RecentSalesVolume = 210, IsLowStock = true }
    ];

    private static List<InventoryItemDto> GetMockInventory() =>
    [
        new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 450, ReorderPoint = 100, RecentSalesVolume = 980, IsLowStock = false },
        new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 22, ReorderPoint = 80, RecentSalesVolume = 210, IsLowStock = true },
        new() { ItemId = "i3", ItemName = "Tegels 60x60 Grijs", Barcode = "8712345000035", QuantityOnHand = 210, ReorderPoint = 50, RecentSalesVolume = 760, IsLowStock = false }
    ];

    public Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InventoryItemDto>>(GetMockInventory());

    public Task<IReadOnlyList<InventoryItemDto>> GetTopSellingItemsAsync(DateRangeFilter filter, int limit, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InventoryItemDto> items =
        [
            new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 450, RecentSalesVolume = 980, IsLowStock = false },
            new() { ItemId = "i3", ItemName = "Tegels 60x60 Grijs", Barcode = "8712345000035", QuantityOnHand = 210, RecentSalesVolume = 760, IsLowStock = false },
            new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 180, RecentSalesVolume = 640, IsLowStock = false }
        ];

        return Task.FromResult<IReadOnlyList<InventoryItemDto>>(items.Take(limit).ToList());
    }

    public Task<IReadOnlyList<StockLocationDto>> GetItemLocationsAsync(string itemId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StockLocationDto> locations =
        [
            new() { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300 },
            new() { LocationId = "loc2", LocationName = "Terrein A", QuantityOnHand = 150 }
        ];

        return Task.FromResult(locations);
    }

    public Task<IReadOnlyList<OpenTransactionDto>> GetOpenTransactionsAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var transactions = GetMockTransactions().Where(t => t.ItemId == itemId).ToList();
        return Task.FromResult<IReadOnlyList<OpenTransactionDto>>(transactions);
    }

    private static List<OpenTransactionDto> GetMockTransactions() =>
    [
        new() { TransactionId = "t1", Type = "Sales Order", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", TransactionDate = Today.AddDays(-1), QuantityOpen = 40, Status = "Pending Fulfillment" },
        new() { TransactionId = "t2", Type = "Transfer Order", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", TransactionDate = Today.AddDays(-2), QuantityOpen = 20, Status = "Pending Receipt" },
        new() { TransactionId = "t3", Type = "Sales Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", TransactionDate = Today.AddDays(-5), QuantityOpen = 55, Status = "Pending Fulfillment" }
    ];

    public Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var expenses = GetMockExpenses().Where(e => e.Date >= filter.From && e.Date <= filter.To).ToList();
        return Task.FromResult<IReadOnlyList<ExpenseDto>>(expenses);
    }

    private static List<ExpenseDto> GetMockExpenses() =>
    [
        new() { Id = "e1", Date = Today.AddDays(-20), Category = "Transport", Amount = 12500m, VendorName = "Logistics Partner BV" },
        new() { Id = "e2", Date = Today.AddDays(-10), Category = "Utilities", Amount = 4200m, VendorName = "Energiebedrijf NL" },
        new() { Id = "e3", Date = Today.AddDays(-2), Category = "Maintenance", Amount = 2950m, VendorName = "Technische Dienst BV" }
    ];

    public Task<ItemDetailDto?> GetItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (barcode != "8712345000011")
        {
            return Task.FromResult<ItemDetailDto?>(null);
        }

        var detail = new ItemDetailDto
        {
            ItemId = "i1",
            ItemName = "Betonstraatstenen Antraciet",
            Barcode = barcode,
            TotalQuantityOnHand = 450,
            ReorderPoint = 100,
            RecentSalesVolume = 980,
            IsLowStock = false,
            Locations =
            [
                new StockLocationDto { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300 },
                new StockLocationDto { LocationId = "loc2", LocationName = "Terrein A", QuantityOnHand = 150 }
            ],
            OpenTransactionCount = 2,
            OpenTransactions =
            [
                new OpenTransactionDto
                {
                    TransactionId = "t1",
                    Type = "Sales Order",
                    ItemId = "i1",
                    ItemName = "Betonstraatstenen Antraciet",
                    TransactionDate = Today.AddDays(-1),
                    QuantityOpen = 40,
                    Status = "Pending Fulfillment"
                }
            ]
        };

        return Task.FromResult<ItemDetailDto?>(detail);
    }

    public Task<ItemDetailDto?> GetItemByIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (itemId != "i1")
        {
            return Task.FromResult<ItemDetailDto?>(null);
        }

        return GetItemByBarcodeAsync("8712345000011", cancellationToken);
    }
}
