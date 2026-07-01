using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.Data.Entities;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Imports NetSuite master and transactional data into the dashboard database.
/// </summary>
public sealed class NetSuiteSyncService(
    TebiDashboardDbContext dbContext,
    INetSuiteRestClient restClient,
    IOptions<NetSuiteOptions> options,
    ILogger<NetSuiteSyncService> logger) : INetSuiteSyncService
{
    public async Task<NetSuiteSyncResult> SyncDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            return Failed(settings.DescribeMissingConfiguration()
                + " Set NetSuite environment variables on the API service (e.g. NetSuite__Enabled=true).");
        }

        var lookbackFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Math.Max(settings.SyncLookbackDays, 1)));
        var pageSize = settings.SyncPageSize;

        try
        {
            if (settings.ClearDashboardDataOnSync)
            {
                await RunStepAsync("clear dashboard tables", () => ClearDashboardTablesAsync(cancellationToken));
            }

            var customers = await RunStepAsync("customers", SyncCustomersAsync, pageSize, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var suppliers = await RunStepAsync("suppliers", SyncSuppliersAsync, pageSize, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var inventoryItems = await RunStepAsync("inventory items", SyncInventoryItemsAsync, pageSize, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var stockLocations = await RunStepAsync("stock locations", SyncStockLocationsAsync, pageSize, cancellationToken);
            await ApplyInventoryQuantitiesFromStockLocationsAsync(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var revenue = await RunStepAsync("invoices", SyncRevenueAsync, lookbackFrom, pageSize, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var expenses = await RunStepAsync("vendor bills", SyncExpensesAsync, lookbackFrom, pageSize, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var openTransactions = await RunStepAsync(
                "open sales and purchase orders",
                SyncOpenTransactionsAsync,
                lookbackFrom,
                pageSize,
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await RunStepAsync(
                "recent item sales",
                ct => ApplyRecentSalesVolumeAsync(lookbackFrom, pageSize, ct),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new NetSuiteSyncResult(
                Success: true,
                Message: "NetSuite data imported successfully.",
                CustomersSynced: customers,
                SuppliersSynced: suppliers,
                InventoryItemsSynced: inventoryItems,
                StockLocationsSynced: stockLocations,
                RevenueRecordsSynced: revenue,
                ExpenseRecordsSynced: expenses,
                OpenTransactionsSynced: openTransactions,
                CompletedAt: DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NetSuite dashboard sync failed.");
            var message = ex.InnerException?.Message ?? ex.Message;
            return Failed(message);
        }
    }

    private async Task<int> RunStepAsync(string stepName, Func<Task<int>> step)
    {
        try
        {
            logger.LogInformation("NetSuite sync step started: {StepName}", stepName);
            var count = await step();
            logger.LogInformation("NetSuite sync step completed: {StepName} ({Count} rows)", stepName, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NetSuite sync step failed: {StepName}", stepName);
            throw new InvalidOperationException($"Sync step '{stepName}' failed: {ex.Message}", ex);
        }
    }

    private async Task RunStepAsync(string stepName, Func<Task> step)
    {
        try
        {
            logger.LogInformation("NetSuite sync step started: {StepName}", stepName);
            await step();
            logger.LogInformation("NetSuite sync step completed: {StepName}", stepName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NetSuite sync step failed: {StepName}", stepName);
            throw new InvalidOperationException($"Sync step '{stepName}' failed: {ex.Message}", ex);
        }
    }

    private Task<int> RunStepAsync(
        string stepName,
        Func<int, CancellationToken, Task<int>> step,
        int pageSize,
        CancellationToken cancellationToken) =>
        RunStepAsync(stepName, () => step(pageSize, cancellationToken));

    private Task<int> RunStepAsync(
        string stepName,
        Func<DateOnly, int, CancellationToken, Task<int>> step,
        DateOnly fromDate,
        int pageSize,
        CancellationToken cancellationToken) =>
        RunStepAsync(stepName, () => step(fromDate, pageSize, cancellationToken));

    private Task RunStepAsync(
        string stepName,
        Func<CancellationToken, Task> step,
        CancellationToken cancellationToken) =>
        RunStepAsync(stepName, () => step(cancellationToken));

    private async Task ClearDashboardTablesAsync(CancellationToken cancellationToken)
    {
        // Keep users and locally configured goals; replace imported dashboard facts.
        await dbContext.StockLocations.ExecuteDeleteAsync(cancellationToken);
        await dbContext.OpenTransactions.ExecuteDeleteAsync(cancellationToken);
        await dbContext.RevenueRecords.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Expenses.ExecuteDeleteAsync(cancellationToken);
        await dbContext.InventoryItems.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Suppliers.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Customers.ExecuteDeleteAsync(cancellationToken);

        // Ensure no stale tracked entities remain after bulk deletes.
        dbContext.ChangeTracker.Clear();
    }

    private async Task<int> SyncCustomersAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Customers(), pageSize, cancellationToken),
            "id");
        foreach (var row in rows)
        {
            dbContext.Customers.Add(new CustomerRecord
            {
                CustomerId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id"),
                Name = NetSuiteSuiteQlRowReader.GetString(row, "companyname") ?? "Unknown customer",
                // City lives on NetSuite address subrecords; left empty until a dedicated address query is added.
                City = string.Empty,
                ContactEmail = NetSuiteSuiteQlRowReader.GetString(row, "email") ?? string.Empty
            });
        }

        return rows.Count;
    }

    private async Task<int> SyncSuppliersAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Vendors(), pageSize, cancellationToken),
            "id");
        foreach (var row in rows)
        {
            dbContext.Suppliers.Add(new SupplierRecord
            {
                SupplierId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id"),
                Name = NetSuiteSuiteQlRowReader.GetString(row, "companyname") ?? "Unknown supplier",
                // City lives on NetSuite address subrecords; left empty until a dedicated address query is added.
                City = string.Empty,
                ContactEmail = NetSuiteSuiteQlRowReader.GetString(row, "email") ?? string.Empty
            });
        }

        return rows.Count;
    }

    private async Task<int> SyncInventoryItemsAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.InventoryItems(), pageSize, cancellationToken),
            "id");
        foreach (var row in rows)
        {
            dbContext.InventoryItems.Add(new InventoryItemRecord
            {
                ItemId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id"),
                ItemName = NetSuiteSuiteQlRowReader.GetString(row, "displayname") ?? "Unknown item",
                Barcode = NetSuiteSuiteQlRowReader.GetString(row, "upccode") ?? string.Empty,
                QuantityOnHand = 0m,
                // Reorder point is account-specific in NetSuite; default to 0 until mapped from a custom field.
                ReorderPoint = 0m,
                RecentSalesVolume = 0m,
                IsLowStock = false
            });
        }

        return rows.Count;
    }

    private async Task<int> SyncStockLocationsAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByFields(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.StockLocations(), pageSize, cancellationToken),
            "item",
            "location");
        foreach (var row in rows)
        {
            dbContext.StockLocations.Add(new StockLocationRecord
            {
                ItemId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "item"),
                LocationId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "location"),
                LocationName = NetSuiteSuiteQlRowReader.GetString(row, "locationname")
                    ?? NetSuiteSuiteQlRowReader.GetString(row, "location")
                    ?? "Unknown location",
                QuantityOnHand = NetSuiteSuiteQlRowReader.GetDecimal(row, "quantityonhand")
            });
        }

        return rows.Count;
    }

    private async Task<int> SyncRevenueAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Invoices(fromDate), pageSize, cancellationToken),
            "id");
        foreach (var row in rows)
        {
            dbContext.RevenueRecords.Add(new RevenueRecord
            {
                Id = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id"),
                Date = NetSuiteSuiteQlRowReader.GetDate(row, "trandate"),
                Amount = NetSuiteSuiteQlRowReader.GetDecimal(row, "amount"),
                CustomerId = NetSuiteSuiteQlRowReader.GetString(row, "customerid") ?? string.Empty,
                CustomerName = NetSuiteSuiteQlRowReader.GetString(row, "customername") ?? string.Empty,
                ProductGroupId = NetSuiteSuiteQlRowReader.GetString(row, "productgroupid") ?? string.Empty
            });
        }

        return rows.Count;
    }

    private async Task<int> SyncExpensesAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.VendorBills(fromDate), pageSize, cancellationToken),
            "id");
        foreach (var row in rows)
        {
            dbContext.Expenses.Add(new ExpenseRecord
            {
                Id = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id"),
                Date = NetSuiteSuiteQlRowReader.GetDate(row, "trandate"),
                Amount = NetSuiteSuiteQlRowReader.GetDecimal(row, "amount"),
                Category = "Vendor Bill",
                VendorName = NetSuiteSuiteQlRowReader.GetString(row, "vendorname") ?? string.Empty,
                SupplierId = NetSuiteSuiteQlRowReader.GetString(row, "supplierid")
            });
        }

        return rows.Count;
    }

    private async Task<int> SyncOpenTransactionsAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var salesRows = NetSuiteSuiteQlRowDeduplicator.ByFields(
            await restClient.QuerySuiteQlAsync(
                NetSuiteSuiteQlQueries.OpenSalesOrderLines(fromDate),
                pageSize,
                cancellationToken),
            "transaction",
            "lineid");

        foreach (var row in salesRows)
        {
            dbContext.OpenTransactions.Add(MapOpenTransactionRow(
                row,
                type: "Sales Order",
                customerId: NetSuiteSuiteQlRowReader.GetString(row, "customerid"),
                supplierId: null));
        }

        var purchaseRows = NetSuiteSuiteQlRowDeduplicator.ByFields(
            await restClient.QuerySuiteQlAsync(
                NetSuiteSuiteQlQueries.OpenPurchaseOrderLines(fromDate),
                pageSize,
                cancellationToken),
            "transaction",
            "lineid");

        foreach (var row in purchaseRows)
        {
            dbContext.OpenTransactions.Add(MapOpenTransactionRow(
                row,
                type: "Purchase Order",
                customerId: null,
                supplierId: NetSuiteSuiteQlRowReader.GetString(row, "supplierid")));
        }

        return salesRows.Count + purchaseRows.Count;
    }

    private static OpenTransactionRecord MapOpenTransactionRow(
        IReadOnlyDictionary<string, JsonElement> row,
        string type,
        string? customerId,
        string? supplierId)
    {
        // Composite key keeps line-level open orders unique in the dashboard table.
        var transactionId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "transaction");
        var lineId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "lineid");

        return new OpenTransactionRecord
        {
            TransactionId = $"{transactionId}-{lineId}",
            Type = type,
            ItemId = NetSuiteSuiteQlRowReader.GetString(row, "itemid") ?? string.Empty,
            CustomerId = customerId,
            SupplierId = supplierId,
            TransactionDate = NetSuiteSuiteQlRowReader.GetDate(row, "trandate"),
            QuantityOpen = NetSuiteSuiteQlRowReader.GetDecimal(row, "quantityopen"),
            Amount = NetSuiteSuiteQlRowReader.GetDecimal(row, "amount"),
            Status = NetSuiteSuiteQlRowReader.GetString(row, "status") ?? "Open"
        };
    }

    private async Task ApplyInventoryQuantitiesFromStockLocationsAsync(CancellationToken cancellationToken)
    {
        var quantitiesByItem = await dbContext.StockLocations
            .AsNoTracking()
            .GroupBy(location => location.ItemId)
            .Select(group => new { ItemId = group.Key, Quantity = group.Sum(location => location.QuantityOnHand) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity, cancellationToken);

        var items = await dbContext.InventoryItems.ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            var quantityOnHand = quantitiesByItem.GetValueOrDefault(item.ItemId);
            item.QuantityOnHand = quantityOnHand;
            item.IsLowStock = item.ReorderPoint > 0 && quantityOnHand <= item.ReorderPoint;
        }
    }

    private async Task ApplyRecentSalesVolumeAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(
                NetSuiteSuiteQlQueries.RecentItemSales(fromDate),
                pageSize,
                cancellationToken),
            "itemid");

        if (rows.Count == 0)
        {
            return;
        }

        var salesByItem = rows.ToDictionary(
            row => NetSuiteSuiteQlRowReader.GetRequiredString(row, "itemid"),
            row => NetSuiteSuiteQlRowReader.GetDecimal(row, "recentsalesvolume"),
            StringComparer.OrdinalIgnoreCase);

        var items = await dbContext.InventoryItems.ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            if (salesByItem.TryGetValue(item.ItemId, out var soldQuantity))
            {
                item.RecentSalesVolume = soldQuantity;
            }
        }
    }

    private static NetSuiteSyncResult Failed(string message) =>
        new(
            Success: false,
            Message: message,
            CustomersSynced: 0,
            SuppliersSynced: 0,
            InventoryItemsSynced: 0,
            StockLocationsSynced: 0,
            RevenueRecordsSynced: 0,
            ExpenseRecordsSynced: 0,
            OpenTransactionsSynced: 0,
            CompletedAt: DateTimeOffset.UtcNow);
}
