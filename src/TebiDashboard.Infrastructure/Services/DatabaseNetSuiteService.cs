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

    public async Task<IReadOnlyList<CustomerOverviewDto>> GetCustomersOverviewAsync(
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var customers = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var revenueByCustomer = await dbContext.RevenueRecords
            .AsNoTracking()
            .Where(r => r.Date >= filter.From && r.Date <= filter.To)
            .GroupBy(r => r.CustomerId)
            .Select(g => new { CustomerId = g.Key, Total = g.Sum(r => r.Amount), Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        // Open sales orders stay visible even when the order date is outside the selected period.
        var salesOrdersByCustomer = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.Type == "Sales Order"
                && t.CustomerId != null)
            .GroupBy(t => t.CustomerId!)
            .Select(g => new
            {
                CustomerId = g.Key,
                Total = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        return customers.Select(customer =>
        {
            var revenue = revenueByCustomer.GetValueOrDefault(customer.CustomerId);
            var salesOrders = salesOrdersByCustomer.GetValueOrDefault(customer.CustomerId);
            var orderTotal = salesOrders?.Total ?? 0m;

            return new CustomerOverviewDto
            {
                CustomerId = customer.CustomerId,
                Name = customer.Name,
                City = customer.City,
                ContactEmail = customer.ContactEmail,
                Category = customer.Category,
                Subcategory = customer.Subcategory,
                // Total spent follows sales order amounts; demo data keeps revenue in sync.
                TotalSpent = orderTotal > 0 ? orderTotal : revenue?.Total ?? 0m,
                RevenueEntryCount = revenue?.Count ?? 0,
                SalesOrderCount = salesOrders?.Count ?? 0
            };
        }).ToList();
    }

    public async Task<CustomerDetailDto?> GetCustomerDetailAsync(
        string customerId,
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var revenue = await dbContext.RevenueRecords
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId && r.Date >= filter.From && r.Date <= filter.To)
            .OrderByDescending(r => r.Date)
            .ToListAsync(cancellationToken);

        var salesOrderRows = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.Type == "Sales Order"
                && t.CustomerId == customerId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenBy(t => t.TransactionId)
            .ToListAsync(cancellationToken);

        // Look up names for sales-order items and for revenue lines that already store an item id.
        var itemIds = salesOrderRows.Select(r => r.ItemId)
            .Concat(revenue.Select(r => r.ItemId))
            .Concat(revenue.Select(r => RevenueItemLink.TryGetItemId(r.ProductGroupId))
                .Where(id => id is not null)
                .Select(id => id!))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        var itemNames = itemIds.Count == 0
            ? new Dictionary<string, string>()
            : await dbContext.InventoryItems
                .AsNoTracking()
                .Where(i => itemIds.Contains(i.ItemId))
                .ToDictionaryAsync(i => i.ItemId, i => i.ItemName, cancellationToken);

        var customerNames = new Dictionary<string, string> { [customer.CustomerId] = customer.Name };
        var salesOrders = salesOrderRows
            .Select(t => MapOpenTransaction(
                t,
                ResolveStoredOrLookedUpItemName(t.ItemId, t.ItemName, itemNames),
                customerNames,
                new Dictionary<string, string>()))
            .ToList();
        var orderTotal = salesOrders.Sum(order => order.Amount);

        return new CustomerDetailDto
        {
            Customer = MapCustomer(customer),
            PeriodFrom = filter.From,
            PeriodTo = filter.To,
            TotalSpent = orderTotal > 0 ? orderTotal : revenue.Sum(r => r.Amount),
            // MapRevenue fills item names; ProductGroupName comes from the stored artikelgroep.
            Revenue = revenue.Select(r => MapRevenue(r, itemNames, customer.Name)).ToList(),
            SalesOrders = salesOrders
        };
    }

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(MapSupplier).ToList();
    }

    public async Task<IReadOnlyList<SupplierOverviewDto>> GetSuppliersOverviewAsync(
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var suppliers = await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var expensesBySupplier = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.SupplierId != null && e.Date >= filter.From && e.Date <= filter.To)
            .GroupBy(e => e.SupplierId!)
            .Select(g => new { SupplierId = g.Key, Total = g.Sum(e => e.Amount), Count = g.Count() })
            .ToDictionaryAsync(x => x.SupplierId, cancellationToken);

        var purchaseOrdersBySupplier = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.Type == "Purchase Order"
                && t.SupplierId != null
                && t.TransactionDate >= filter.From
                && t.TransactionDate <= filter.To)
            .ToListAsync(cancellationToken);

        var purchaseOrderTotals = purchaseOrdersBySupplier
            .GroupBy(t => t.SupplierId!)
            .ToDictionary(
                g => g.Key,
                g => new { Count = g.Count(), Total = g.Sum(t => t.Amount), Items = g.Select(t => t.ItemId).Distinct().Count() });

        return suppliers.Select(supplier =>
        {
            var expenses = expensesBySupplier.GetValueOrDefault(supplier.SupplierId);
            var purchaseOrders = purchaseOrderTotals.GetValueOrDefault(supplier.SupplierId);
            var expenseTotal = expenses?.Total ?? 0m;
            var purchaseOrderTotal = purchaseOrders?.Total ?? 0m;

            return new SupplierOverviewDto
            {
                SupplierId = supplier.SupplierId,
                Name = supplier.Name,
                City = supplier.City,
                ContactEmail = supplier.ContactEmail,
                TotalPurchased = expenseTotal + purchaseOrderTotal,
                ExpenseEntryCount = expenses?.Count ?? 0,
                PurchaseOrderCount = purchaseOrders?.Count ?? 0,
                PurchasedItemCount = purchaseOrders?.Items ?? 0
            };
        }).ToList();
    }

    public async Task<SupplierDetailDto?> GetSupplierDetailAsync(
        string supplierId,
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var supplier = await dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierId == supplierId, cancellationToken);

        if (supplier is null)
        {
            return null;
        }

        var expenseRows = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.SupplierId == supplierId && e.Date >= filter.From && e.Date <= filter.To)
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);

        var purchaseOrderRows = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.Type == "Purchase Order"
                && t.SupplierId == supplierId
                && t.TransactionDate >= filter.From
                && t.TransactionDate <= filter.To)
            .OrderByDescending(t => t.TransactionDate)
            .ThenBy(t => t.TransactionId)
            .ToListAsync(cancellationToken);

        var itemIds = purchaseOrderRows.Select(r => r.ItemId).Distinct().ToList();
        var itemNames = itemIds.Count == 0
            ? new Dictionary<string, string>()
            : await dbContext.InventoryItems
                .AsNoTracking()
                .Where(i => itemIds.Contains(i.ItemId))
                .ToDictionaryAsync(i => i.ItemId, i => i.ItemName, cancellationToken);

        var customerNames = await LoadCustomerNamesAsync(purchaseOrderRows, cancellationToken);
        var supplierNames = new Dictionary<string, string> { [supplier.SupplierId] = supplier.Name };

        var purchasedItems = purchaseOrderRows
            .GroupBy(t => t.ItemId)
            .Select(group => new SupplierPurchasedItemDto
            {
                ItemId = group.Key,
                ItemName = ResolveStoredOrLookedUpItemName(group.Key, group.First().ItemName, itemNames),
                TotalQuantityOpen = group.Sum(t => t.QuantityOpen)
            })
            .OrderBy(item => item.ItemName)
            .ToList();

        var purchaseOrders = purchaseOrderRows
            .Select(t => MapOpenTransaction(
                t,
                ResolveStoredOrLookedUpItemName(t.ItemId, t.ItemName, itemNames),
                customerNames,
                supplierNames))
            .ToList();

        return new SupplierDetailDto
        {
            Supplier = MapSupplier(supplier),
            PeriodFrom = filter.From,
            PeriodTo = filter.To,
            TotalPurchased = expenseRows.Sum(e => e.Amount) + purchaseOrders.Sum(order => order.Amount),
            Expenses = expenseRows.Select(e => MapExpense(e, supplierNames)).ToList(),
            PurchaseOrders = purchaseOrders,
            PurchasedItems = purchasedItems
        };
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
            InventoryWithActivity = inventoryWithActivity.Select(item => MapInventoryItem(item)).ToList()
        };
    }

    private async Task<IReadOnlyList<OpenTransactionDto>> GetTransactionsInPeriodAsync(
        DateRangeFilter filter,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.Type == "Sales Order"
                || (t.TransactionDate >= filter.From && t.TransactionDate <= filter.To))
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
        var supplierNames = await LoadSupplierNamesAsync(rows, cancellationToken);

        return rows.Select(t => MapOpenTransaction(
            t,
            ResolveStoredOrLookedUpItemName(t.ItemId, t.ItemName, itemNames),
            customerNames,
            supplierNames)).ToList();
    }

    public async Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.RevenueRecords
            .AsNoTracking()
            .Where(r => r.Date >= filter.From && r.Date <= filter.To)
            .OrderByDescending(r => r.Date)
            .ToListAsync(cancellationToken);

        var itemIds = rows
            .Select(r => r.ItemId)
            .Concat(rows.Select(r => RevenueItemLink.TryGetItemId(r.ProductGroupId))
                .Where(id => id is not null)
                .Select(id => id!))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        var itemNames = await LoadItemNamesAsync(itemIds, cancellationToken);

        // Include both artikelgroep name and the representative item so callers can show either.
        return rows.Select(r => MapRevenue(r, itemNames, customerNameFallback: null)).ToList();
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

        return rows.Select(item => MapInventoryItem(item)).ToList();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.InventoryItems
            .AsNoTracking()
            .OrderBy(i => i.ItemName)
            .ToListAsync(cancellationToken);

        // Derive recent sales volume for the selected period from synced open sales orders in Postgres.
        var salesByItem = await dbContext.OpenTransactions
            .AsNoTracking()
            .Where(t => t.Type == "Sales Order"
                && t.TransactionDate >= filter.From
                && t.TransactionDate <= filter.To)
            .GroupBy(t => t.ItemId)
            .Select(g => new { ItemId = g.Key, Volume = g.Sum(t => t.QuantityOpen) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Volume, cancellationToken);

        return rows
            .Select(item => MapInventoryItem(item, salesByItem.GetValueOrDefault(item.ItemId)))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetTopSellingItemsAsync(DateRangeFilter filter, int limit, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.InventoryItems
            .AsNoTracking()
            .OrderByDescending(i => i.RecentSalesVolume)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(item => MapInventoryItem(item)).ToList();
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

        var lookedUpName = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => i.ItemId == itemId)
            .Select(i => i.ItemName)
            .FirstOrDefaultAsync(cancellationToken);
        var storedName = rows.Select(t => t.ItemName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
            ?? string.Empty;
        var itemName = ResolveStoredOrLookedUpItemName(itemId, storedName, lookedUpName is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { [itemId] = lookedUpName });

        var customerNames = await LoadCustomerNamesAsync(rows, cancellationToken);
        var supplierNames = await LoadSupplierNamesAsync(rows, cancellationToken);

        return rows.Select(t => MapOpenTransaction(t, itemName, customerNames, supplierNames)).ToList();
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= filter.From && e.Date <= filter.To)
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);

        var supplierNames = await LoadSupplierNamesByIdAsync(
            rows.Select(e => e.SupplierId),
            cancellationToken);

        return rows.Select(e => MapExpense(e, supplierNames)).ToList();
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

    private async Task<Dictionary<string, string>> LoadSupplierNamesAsync(
        IReadOnlyList<Data.Entities.OpenTransactionRecord> rows,
        CancellationToken cancellationToken)
    {
        var supplierIds = rows
            .Where(t => !string.IsNullOrWhiteSpace(t.SupplierId))
            .Select(t => t.SupplierId!)
            .Distinct()
            .ToList();

        if (supplierIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Suppliers
            .AsNoTracking()
            .Where(s => supplierIds.Contains(s.SupplierId))
            .ToDictionaryAsync(s => s.SupplierId, s => s.Name, cancellationToken);
    }

    private async Task<Dictionary<string, string>> LoadItemNamesAsync(
        IReadOnlyList<string> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return [];
        }

        return await dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.ItemId))
            .ToDictionaryAsync(i => i.ItemId, i => i.ItemName, cancellationToken);
    }

    private async Task<Dictionary<string, string>> LoadSupplierNamesByIdAsync(
        IEnumerable<string?> supplierIds,
        CancellationToken cancellationToken)
    {
        var ids = supplierIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Suppliers
            .AsNoTracking()
            .Where(s => ids.Contains(s.SupplierId))
            .ToDictionaryAsync(s => s.SupplierId, s => s.Name, cancellationToken);
    }

    private static RevenueDto MapRevenue(
        Data.Entities.RevenueRecord record,
        IReadOnlyDictionary<string, string> itemNames,
        string? customerNameFallback)
    {
        var linkedItemId = string.IsNullOrWhiteSpace(record.ItemId)
            ? RevenueItemLink.TryGetItemId(record.ProductGroupId) ?? string.Empty
            : record.ItemId;
        var itemName = ResolveStoredOrLookedUpItemName(linkedItemId, record.ItemName, itemNames);
        if (string.IsNullOrWhiteSpace(itemName) && string.IsNullOrWhiteSpace(record.ItemId))
        {
            // Demo rows that only have a product-group code still resolve through RevenueItemLink.
            itemName = RevenueItemLink.ResolveItemName(record.ProductGroupId, itemNames, out _);
        }

        return new RevenueDto
        {
            Id = record.Id,
            Date = record.Date,
            Amount = record.Amount,
            CustomerId = record.CustomerId,
            // Keep a stored customer name; fill it from the customer master when the row is blank.
            CustomerName = string.IsNullOrWhiteSpace(record.CustomerName)
                ? customerNameFallback ?? string.Empty
                : record.CustomerName,
            ProductGroupId = record.ProductGroupId,
            ProductGroupName = record.ProductGroupName,
            ItemId = linkedItemId,
            ItemName = itemName,
            InvoiceId = record.InvoiceId,
            InvoiceNumber = record.InvoiceNumber
        };
    }

    /// <summary>
    /// Prefer a live inventory name, then the name stored at sync/seed time.
    /// Never return a bare numeric internal id such as "16".
    /// </summary>
    private static string ResolveStoredOrLookedUpItemName(
        string itemId,
        string storedName,
        IReadOnlyDictionary<string, string> itemNames)
    {
        if (!string.IsNullOrWhiteSpace(itemId)
            && itemNames.TryGetValue(itemId, out var lookedUp)
            && !string.IsNullOrWhiteSpace(lookedUp))
        {
            return lookedUp;
        }

        if (!string.IsNullOrWhiteSpace(storedName))
        {
            return storedName;
        }

        return LooksLikeInternalId(itemId) ? string.Empty : itemId;
    }

    private static bool LooksLikeInternalId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);

    private static ExpenseDto MapExpense(
        Data.Entities.ExpenseRecord record,
        IReadOnlyDictionary<string, string> supplierNames)
    {
        // SupplierName is the master-record name. Fall back to the id so the row still
        // identifies the supplier when the vendor text on the bill is empty.
        var supplierName = string.IsNullOrWhiteSpace(record.SupplierId)
            ? string.Empty
            : supplierNames.GetValueOrDefault(record.SupplierId, record.SupplierId);

        return new ExpenseDto
        {
            Id = record.Id,
            Date = record.Date,
            Category = record.Category,
            Amount = record.Amount,
            VendorName = record.VendorName,
            SupplierId = record.SupplierId,
            SupplierName = supplierName
        };
    }

    private static OpenTransactionDto MapOpenTransaction(
        Data.Entities.OpenTransactionRecord record,
        string itemName,
        IReadOnlyDictionary<string, string> customerNames,
        IReadOnlyDictionary<string, string> supplierNames) => new()
    {
        TransactionId = record.TransactionId,
        Type = record.Type,
        ItemId = record.ItemId,
        ItemName = itemName,
        CustomerId = record.CustomerId,
        CustomerName = record.CustomerId is null
            ? string.Empty
            : customerNames.GetValueOrDefault(record.CustomerId, record.CustomerId),
        SupplierId = record.SupplierId,
        SupplierName = record.SupplierId is null
            ? string.Empty
            : supplierNames.GetValueOrDefault(record.SupplierId, record.SupplierId),
        TransactionDate = record.TransactionDate,
        QuantityOpen = record.QuantityOpen,
        Amount = record.Amount,
        Status = record.Status
    };

    private static CustomerDto MapCustomer(Data.Entities.CustomerRecord customer) => new()
    {
        CustomerId = customer.CustomerId,
        Name = customer.Name,
        City = customer.City,
        ContactEmail = customer.ContactEmail,
        Category = customer.Category,
        Subcategory = customer.Subcategory
    };

    private static SupplierDto MapSupplier(Data.Entities.SupplierRecord supplier) => new()
    {
        SupplierId = supplier.SupplierId,
        Name = supplier.Name,
        City = supplier.City,
        ContactEmail = supplier.ContactEmail
    };

    private static InventoryItemDto MapInventoryItem(Data.Entities.InventoryItemRecord item) =>
        MapInventoryItem(item, recentSalesVolume: null);

    private static InventoryItemDto MapInventoryItem(
        Data.Entities.InventoryItemRecord item,
        decimal? recentSalesVolume) => new()
    {
        ItemId = item.ItemId,
        ItemName = item.ItemName,
        Barcode = item.Barcode,
        QuantityOnHand = item.QuantityOnHand,
        ReorderPoint = item.ReorderPoint,
        RecentSalesVolume = recentSalesVolume ?? item.RecentSalesVolume,
        IsLowStock = item.IsLowStock
    };
}
