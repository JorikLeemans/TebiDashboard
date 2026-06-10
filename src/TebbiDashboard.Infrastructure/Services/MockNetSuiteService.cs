using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Application.Options;
using TebbiDashboard.Shared.Dtos;
using TebbiDashboard.Shared.Filters;

namespace TebbiDashboard.Infrastructure.Services;

/// <summary>
/// Mock NetSuite implementation with sample TEBI-like data for development and tests.
/// </summary>
public sealed class MockNetSuiteService : INetSuiteService
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var summary = new DashboardSummaryDto
        {
            Revenue = 1_250_000m,
            SalesVolume = 8420m,
            GoalPercentage = 87.5m,
            RevenueTrend = "up",
            LowStockAlertCount = 3,
            PeriodFrom = filter.From,
            PeriodTo = filter.To
        };

        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RevenueDto> data =
        [
            new() { Id = "1", Date = Today.AddDays(-30), Amount = 42000m, CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", ProductGroupId = "pg1" },
            new() { Id = "2", Date = Today.AddDays(-15), Amount = 38500m, CustomerId = "c2", CustomerName = "Gemeente Utrecht", ProductGroupId = "pg2" },
            new() { Id = "3", Date = Today.AddDays(-7), Amount = 51200m, CustomerId = "c3", CustomerName = "Stratenmakers BV", ProductGroupId = "pg1" }
        ];

        return Task.FromResult(data);
    }

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
    {
        IReadOnlyList<InventoryItemDto> items =
        [
            new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 45, ReorderPoint = 100, RecentSalesVolume = 320, IsLowStock = true },
            new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 22, ReorderPoint = 80, RecentSalesVolume = 210, IsLowStock = true }
        ];

        return Task.FromResult(items);
    }

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
        IReadOnlyList<OpenTransactionDto> transactions =
        [
            new() { TransactionId = "t1", Type = "Sales Order", ItemId = itemId, QuantityOpen = 40, Status = "Pending Fulfillment" },
            new() { TransactionId = "t2", Type = "Transfer Order", ItemId = itemId, QuantityOpen = 20, Status = "Pending Receipt" }
        ];

        return Task.FromResult(transactions);
    }

    public Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExpenseDto> expenses =
        [
            new() { Id = "e1", Date = Today.AddDays(-20), Category = "Transport", Amount = 12500m, VendorName = "Logistics Partner BV" },
            new() { Id = "e2", Date = Today.AddDays(-10), Category = "Utilities", Amount = 4200m, VendorName = "Energiebedrijf NL" }
        ];

        return Task.FromResult(expenses);
    }

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
            Locations =
            [
                new StockLocationDto { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300 },
                new StockLocationDto { LocationId = "loc2", LocationName = "Terrein A", QuantityOnHand = 150 }
            ],
            OpenTransactionCount = 2,
            OpenTransactions =
            [
                new OpenTransactionDto { TransactionId = "t1", Type = "Sales Order", ItemId = "i1", QuantityOpen = 40, Status = "Pending Fulfillment" }
            ]
        };

        return Task.FromResult<ItemDetailDto?>(detail);
    }
}
