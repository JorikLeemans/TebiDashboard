using Microsoft.EntityFrameworkCore;
using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Infrastructure.Data;
using TebbiDashboard.Shared.Dtos;
using TebbiDashboard.Shared.Filters;

namespace TebbiDashboard.Infrastructure.Services;

/// <summary>
/// Reads dashboard data from the database instead of hard-coded mock values.
/// </summary>
public sealed class DatabaseNetSuiteService(TebbiDashboardDbContext dbContext) : INetSuiteService
{
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var revenueRows = await dbContext.RevenueRecords
            .AsNoTracking()
            .Where(r => r.Date >= filter.From && r.Date <= filter.To)
            .ToListAsync(cancellationToken);

        var totalRevenue = revenueRows.Sum(r => r.Amount);
        var salesVolume = await dbContext.InventoryItems.AsNoTracking().SumAsync(i => i.RecentSalesVolume, cancellationToken);
        var lowStockCount = await dbContext.InventoryItems.AsNoTracking().CountAsync(i => i.IsLowStock, cancellationToken);

        var year = filter.To.Year;
        var goals = await dbContext.Goals
            .AsNoTracking()
            .Where(g => g.Year == year)
            .ToListAsync(cancellationToken);

        var goalPercentage = goals.Count == 0
            ? 0m
            : goals.Average(g => g.TargetValue == 0 ? 0m : g.ActualValue / g.TargetValue * 100m);

        var midpoint = filter.From.AddDays((filter.To.DayNumber - filter.From.DayNumber) / 2);
        var firstHalf = revenueRows.Where(r => r.Date <= midpoint).Sum(r => r.Amount);
        var secondHalf = revenueRows.Where(r => r.Date > midpoint).Sum(r => r.Amount);
        var trend = secondHalf >= firstHalf ? "up" : "down";

        return new DashboardSummaryDto
        {
            Revenue = totalRevenue,
            SalesVolume = salesVolume,
            GoalPercentage = Math.Round(goalPercentage, 1),
            RevenueTrend = trend,
            LowStockAlertCount = lowStockCount,
            PeriodFrom = filter.From,
            PeriodTo = filter.To
        };
    }

    public async Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.RevenueRecords
            .AsNoTracking()
            .Where(r => r.Date >= filter.From && r.Date <= filter.To)
            .OrderByDescending(r => r.Date)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new RevenueDto
        {
            Id = r.Id,
            Date = r.Date,
            Amount = r.Amount,
            CustomerId = r.CustomerId,
            CustomerName = r.CustomerName,
            ProductGroupId = r.ProductGroupId
        }).ToList();
    }

    public async Task<IReadOnlyList<GoalDto>> GetGoalsAsync(int year, string? period = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Goals.AsNoTracking().Where(g => g.Year == year);

        if (!string.IsNullOrWhiteSpace(period))
        {
            query = query.Where(g => g.Period == period);
        }

        var rows = await query.OrderBy(g => g.Period).ThenBy(g => g.ItemName).ToListAsync(cancellationToken);

        return rows.Select(g => new GoalDto
        {
            Id = g.Id,
            Year = g.Year,
            Period = g.Period,
            Type = g.Type,
            ItemId = g.ItemId,
            ItemName = g.ItemName,
            TargetValue = g.TargetValue,
            ActualValue = g.ActualValue,
            Percentage = 0,
            Status = g.Status
        }).ToList();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(bool highSalesOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryItems.AsNoTracking().Where(i => i.IsLowStock);

        if (highSalesOnly)
        {
            query = query.Where(i => i.RecentSalesVolume >= 150);
        }

        var rows = await query.OrderByDescending(i => i.RecentSalesVolume).ToListAsync(cancellationToken);

        return rows.Select(MapInventoryItem).ToList();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.InventoryItems
            .AsNoTracking()
            .OrderBy(i => i.ItemName)
            .ToListAsync(cancellationToken);

        return rows.Select(MapInventoryItem).ToList();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetTopSellingItemsAsync(DateRangeFilter filter, int limit, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.InventoryItems
            .AsNoTracking()
            .OrderByDescending(i => i.RecentSalesVolume)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(MapInventoryItem).ToList();
    }

    public async Task<IReadOnlyList<StockLocationDto>> GetItemLocationsAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.StockLocations
            .AsNoTracking()
            .Where(l => l.ItemId == itemId)
            .OrderBy(l => l.LocationName)
            .ToListAsync(cancellationToken);

        return rows.Select(l => new StockLocationDto
        {
            LocationId = l.LocationId,
            LocationName = l.LocationName,
            QuantityOnHand = l.QuantityOnHand
        }).ToList();
    }

    public async Task<IReadOnlyList<OpenTransactionDto>> GetOpenTransactionsAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.ItemId == itemId)
            .OrderBy(t => t.Type)
            .ToListAsync(cancellationToken);

        return rows.Select(t => new OpenTransactionDto
        {
            TransactionId = t.TransactionId,
            Type = t.Type,
            ItemId = t.ItemId,
            QuantityOpen = t.QuantityOpen,
            Status = t.Status
        }).ToList();
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= filter.From && e.Date <= filter.To)
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);

        return rows.Select(e => new ExpenseDto
        {
            Id = e.Id,
            Date = e.Date,
            Category = e.Category,
            Amount = e.Amount,
            VendorName = e.VendorName
        }).ToList();
    }

    public async Task<ItemDetailDto?> GetItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Barcode == barcode, cancellationToken);
        return item is null ? null : await BuildItemDetailAsync(item, cancellationToken);
    }

    public async Task<ItemDetailDto?> GetItemByIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.ItemId == itemId, cancellationToken);
        return item is null ? null : await BuildItemDetailAsync(item, cancellationToken);
    }

    private async Task<ItemDetailDto> BuildItemDetailAsync(Data.Entities.InventoryItemRecord item, CancellationToken cancellationToken)
    {
        var locations = await GetItemLocationsAsync(item.ItemId, cancellationToken);
        var transactions = await GetOpenTransactionsAsync(item.ItemId, cancellationToken);

        return new ItemDetailDto
        {
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            Barcode = item.Barcode,
            TotalQuantityOnHand = item.QuantityOnHand,
            ReorderPoint = item.ReorderPoint,
            RecentSalesVolume = item.RecentSalesVolume,
            IsLowStock = item.IsLowStock,
            Locations = locations.ToList(),
            OpenTransactionCount = transactions.Count,
            OpenTransactions = transactions.ToList()
        };
    }

    private static InventoryItemDto MapInventoryItem(Data.Entities.InventoryItemRecord item) => new()
    {
        ItemId = item.ItemId,
        ItemName = item.ItemName,
        Barcode = item.Barcode,
        QuantityOnHand = item.QuantityOnHand,
        ReorderPoint = item.ReorderPoint,
        RecentSalesVolume = item.RecentSalesVolume,
        IsLowStock = item.IsLowStock
    };
}
