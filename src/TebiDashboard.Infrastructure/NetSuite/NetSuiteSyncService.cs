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
                await ClearDashboardTablesAsync(cancellationToken);
            }

            var customers = await SyncCustomersAsync(pageSize, cancellationToken);
            var suppliers = await SyncSuppliersAsync(pageSize, cancellationToken);
            var inventoryItems = await SyncInventoryItemsAsync(pageSize, cancellationToken);
            var stockLocations = await SyncStockLocationsAsync(pageSize, cancellationToken);
            var revenue = await SyncRevenueAsync(lookbackFrom, pageSize, cancellationToken);
            var expenses = await SyncExpensesAsync(lookbackFrom, pageSize, cancellationToken);
            var openTransactions = await SyncOpenTransactionsAsync(lookbackFrom, pageSize, cancellationToken);

            await ApplyRecentSalesVolumeAsync(lookbackFrom, pageSize, cancellationToken);
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
            return Failed(ex.Message);
        }
    }

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
    }

    private async Task<int> SyncCustomersAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Customers(), pageSize, cancellationToken);
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
        var rows = await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Vendors(), pageSize, cancellationToken);
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
        var rows = await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.InventoryItems(), pageSize, cancellationToken);
        foreach (var row in rows)
        {
            var quantityOnHand = NetSuiteSuiteQlRowReader.GetDecimal(row, "quantityonhand");
            var reorderPoint = NetSuiteSuiteQlRowReader.GetDecimal(row, "reorderpoint");

            dbContext.InventoryItems.Add(new InventoryItemRecord
            {
                ItemId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id"),
                ItemName = NetSuiteSuiteQlRowReader.GetString(row, "displayname") ?? "Unknown item",
                Barcode = NetSuiteSuiteQlRowReader.GetString(row, "upccode") ?? string.Empty,
                QuantityOnHand = quantityOnHand,
                ReorderPoint = reorderPoint,
                RecentSalesVolume = 0m,
                IsLowStock = reorderPoint > 0 && quantityOnHand <= reorderPoint
            });
        }

        return rows.Count;
    }

    private async Task<int> SyncStockLocationsAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.StockLocations(), pageSize, cancellationToken);
        foreach (var row in rows)
        {
            dbContext.StockLocations.Add(new StockLocationRecord
            {
                ItemId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "item"),
                LocationId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "location"),
                LocationName = NetSuiteSuiteQlRowReader.GetString(row, "locationname") ?? "Unknown location",
                QuantityOnHand = NetSuiteSuiteQlRowReader.GetDecimal(row, "quantityonhand")
            });
        }

        return rows.Count;
    }

    private async Task<int> SyncRevenueAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var rows = await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Invoices(fromDate), pageSize, cancellationToken);
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
        var rows = await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.VendorBills(fromDate), pageSize, cancellationToken);
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
        var salesRows = await restClient.QuerySuiteQlAsync(
            NetSuiteSuiteQlQueries.OpenSalesOrderLines(fromDate),
            pageSize,
            cancellationToken);

        foreach (var row in salesRows)
        {
            dbContext.OpenTransactions.Add(MapOpenTransactionRow(
                row,
                type: "Sales Order",
                customerId: NetSuiteSuiteQlRowReader.GetString(row, "customerid"),
                supplierId: null));
        }

        var purchaseRows = await restClient.QuerySuiteQlAsync(
            NetSuiteSuiteQlQueries.OpenPurchaseOrderLines(fromDate),
            pageSize,
            cancellationToken);

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

    private async Task ApplyRecentSalesVolumeAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var rows = await restClient.QuerySuiteQlAsync(
            NetSuiteSuiteQlQueries.RecentItemSales(fromDate),
            pageSize,
            cancellationToken);

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
