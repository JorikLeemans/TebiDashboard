using Microsoft.EntityFrameworkCore;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// Reads dashboard data from the database instead of hard-coded mock values.
/// </summary>
public sealed class DatabaseNetSuiteService(TebiDashboardDbContext dbContext) : INetSuiteService
{
    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(MapCustomer).ToList();
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var revenueRows = await dbContext.RevenueRecords
            .AsNoTracking()
            .Where(r => r.Date >= filter.From && r.Date <= filter.To)
            .ToListAsync(cancellationToken);

        var totalRevenue = revenueRows.Sum(r => r.Amount);

        // Sales volume in the selected period: sum of open transaction quantities, fallback to revenue line count.
        var salesVolume = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.TransactionDate >= filter.From && t.TransactionDate <= filter.To)
            .SumAsync(t => t.QuantityOpen, cancellationToken);

        if (salesVolume == 0)
        {
            salesVolume = revenueRows.Count;
        }

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

    public async Task<PeriodOverviewDto> GetPeriodOverviewAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var summary = await GetDashboardSummaryAsync(filter, cancellationToken);
        var revenue = await GetRevenueAsync(filter, cancellationToken);
        var expenses = await GetExpensesAsync(filter, cancellationToken);
        var transactions = await GetTransactionsInPeriodAsync(filter, cancellationToken);
        var lowStock = await GetLowStockItemsAsync(highSalesOnly: false, cancellationToken);

        var activeItemIds = transactions.Select(t => t.ItemId).Distinct().ToHashSet();
        var inventoryWithActivity = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => activeItemIds.Contains(i.ItemId))
            .OrderBy(i => i.ItemName)
            .ToListAsync(cancellationToken);

        var customerIdsInPeriod = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.CustomerId))
            .Select(t => t.CustomerId!)
            .Distinct()
            .ToHashSet();

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(c => customerIdsInPeriod.Contains(c.CustomerId))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return new PeriodOverviewDto
        {
            PeriodFrom = filter.From,
            PeriodTo = filter.To,
            Summary = summary,
            Customers = customers.Select(MapCustomer).ToList(),
            Revenue = revenue,
            Expenses = expenses,
            Transactions = transactions,
            LowStockItems = lowStock,
            InventoryWithActivity = inventoryWithActivity.Select(MapInventoryItem).ToList()
        };
    }

    private async Task<IReadOnlyList<OpenTransactionDto>> GetTransactionsInPeriodAsync(
        DateRangeFilter filter,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.TransactionDate >= filter.From && t.TransactionDate <= filter.To)
            .OrderByDescending(t => t.TransactionDate)
            .ThenBy(t => t.Type)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var itemIds = rows.Select(r => r.ItemId).Distinct().ToList();
        var itemNames = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.ItemId))
            .ToDictionaryAsync(i => i.ItemId, i => i.ItemName, cancellationToken);

        var customerNames = await LoadCustomerNamesAsync(rows, cancellationToken);

        return rows.Select(t => MapOpenTransaction(
            t,
            itemNames.GetValueOrDefault(t.ItemId, t.ItemId),
            customerNames)).ToList();
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
            .OrderByDescending(t => t.TransactionDate)
            .ThenBy(t => t.Type)
            .ToListAsync(cancellationToken);

        var itemName = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => i.ItemId == itemId)
            .Select(i => i.ItemName)
            .FirstOrDefaultAsync(cancellationToken) ?? itemId;

        var customerNames = await LoadCustomerNamesAsync(rows, cancellationToken);

        return rows.Select(t => MapOpenTransaction(t, itemName, customerNames)).ToList();
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

    private async Task<Dictionary<string, string>> LoadCustomerNamesAsync(
        IReadOnlyList<Data.Entities.OpenTransactionRecord> rows,
        CancellationToken cancellationToken)
    {
        var customerIds = rows
            .Where(t => !string.IsNullOrWhiteSpace(t.CustomerId))
            .Select(t => t.CustomerId!)
            .Distinct()
            .ToList();

        if (customerIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.CustomerId))
            .ToDictionaryAsync(c => c.CustomerId, c => c.Name, cancellationToken);
    }

    private static OpenTransactionDto MapOpenTransaction(
        Data.Entities.OpenTransactionRecord record,
        string itemName,
        IReadOnlyDictionary<string, string> customerNames) => new()
    {
        TransactionId = record.TransactionId,
        Type = record.Type,
        ItemId = record.ItemId,
        ItemName = itemName,
        CustomerId = record.CustomerId,
        CustomerName = record.CustomerId is null
            ? string.Empty
            : customerNames.GetValueOrDefault(record.CustomerId, record.CustomerId),
        TransactionDate = record.TransactionDate,
        QuantityOpen = record.QuantityOpen,
        Status = record.Status
    };

    private static CustomerDto MapCustomer(Data.Entities.CustomerRecord customer) => new()
    {
        CustomerId = customer.CustomerId,
        Name = customer.Name,
        City = customer.City,
        ContactEmail = customer.ContactEmail
    };

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
