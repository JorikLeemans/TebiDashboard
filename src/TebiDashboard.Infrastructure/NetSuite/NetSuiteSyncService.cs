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
/// Sync is incremental by default: existing revenue, expenses, and masters stay,
/// and only unseen rows are inserted (masters and stock quantities are refreshed).
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
            // Full replace is opt-in. Normal syncs keep stored rows and only add or refresh what NetSuite returns.
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
            // Persist stock first so the on-hand rollup reads the rows just imported or refreshed.
            await dbContext.SaveChangesAsync(cancellationToken);
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
            // Keep the sync step name in the message shown to admins (inner exception hides it).
            var message = ex.Message.StartsWith("Sync step '", StringComparison.Ordinal)
                ? ex.Message
                : ex.InnerException?.Message ?? ex.Message;
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
        var existing = await dbContext.Customers.ToDictionaryAsync(
            customer => customer.CustomerId,
            cancellationToken);
        var imported = 0;
        foreach (var row in rows)
        {
            var customerId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id");
            // Prefer glommedName so individual customers show a real name, not a blank companyName.
            var name = NetSuiteSuiteQlRowReader.GetString(row, "glommedname", "glommedName", "companyname", "companyName")
                ?? "Unknown customer";
            // City comes from entityAddress.city via defaultAddress, not the full address blob.
            var city = NetSuiteSuiteQlRowReader.GetString(row, "city") ?? string.Empty;
            var contactEmail = NetSuiteSuiteQlRowReader.GetString(row, "email") ?? string.Empty;
            // BUILTIN.DF aliases; drop a leftover numeric id so the UI never shows "16".
            var category = ListFieldDisplayName(NetSuiteSuiteQlRowReader.GetString(row, "category"));
            var subcategory = ListFieldDisplayName(
                NetSuiteSuiteQlRowReader.GetString(row, "subcategory", "custentity_tebi_klant_subcategorie"));

            if (existing.TryGetValue(customerId, out var customer))
            {
                // Refresh the stored name/contact; do not delete customers missing from this page.
                customer.Name = name;
                customer.City = city;
                customer.ContactEmail = contactEmail;
                customer.Category = category;
                customer.Subcategory = subcategory;
            }
            else
            {
                var created = new CustomerRecord
                {
                    // Internal id matches invoice.entity; entityId is the human-readable customer code in NetSuite.
                    CustomerId = customerId,
                    Name = name,
                    City = city,
                    ContactEmail = contactEmail,
                    Category = category,
                    Subcategory = subcategory
                };
                dbContext.Customers.Add(created);
                existing[customerId] = created;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> SyncSuppliersAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Vendors(), pageSize, cancellationToken),
            "id");
        var existing = await dbContext.Suppliers.ToDictionaryAsync(
            supplier => supplier.SupplierId,
            cancellationToken);
        var imported = 0;
        foreach (var row in rows)
        {
            var supplierId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id");
            var name = NetSuiteSuiteQlRowReader.GetString(row, "companyname", "companyName") ?? "Unknown supplier";
            // Same city mapping as customers: entityAddress.city joined on defaultAddress.
            var city = NetSuiteSuiteQlRowReader.GetString(row, "city") ?? string.Empty;
            var contactEmail = NetSuiteSuiteQlRowReader.GetString(row, "email") ?? string.Empty;

            if (existing.TryGetValue(supplierId, out var supplier))
            {
                supplier.Name = name;
                supplier.City = city;
                supplier.ContactEmail = contactEmail;
            }
            else
            {
                var created = new SupplierRecord
                {
                    // Internal id matches vendor bill entity; entityId is the human-readable vendor code in NetSuite.
                    SupplierId = supplierId,
                    Name = name,
                    City = city,
                    ContactEmail = contactEmail
                };
                dbContext.Suppliers.Add(created);
                existing[supplierId] = created;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> SyncInventoryItemsAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.InventoryItems(), pageSize, cancellationToken),
            "id");
        var existing = await dbContext.InventoryItems.ToDictionaryAsync(item => item.ItemId, cancellationToken);
        var imported = 0;
        foreach (var row in rows)
        {
            var itemId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id");
            // TEBI uses itemId (SKU) as display name; fall back to displayName when itemId is empty.
            var itemName = NetSuiteSuiteQlRowReader.GetString(row, "itemid", "itemId")
                ?? NetSuiteSuiteQlRowReader.GetString(row, "displayname", "displayName")
                ?? "Unknown item";
            var barcode = NetSuiteSuiteQlRowReader.GetString(row, "upccode", "upcCode") ?? string.Empty;

            if (existing.TryGetValue(itemId, out var item))
            {
                // Keep on-hand and sales figures; those are filled by later snapshot steps.
                item.ItemName = itemName;
                item.Barcode = barcode;
            }
            else
            {
                var created = new InventoryItemRecord
                {
                    ItemId = itemId,
                    ItemName = itemName,
                    Barcode = barcode,
                    QuantityOnHand = 0m,
                    // reorderPoint is not queryable via SuiteQL on item; default to 0 until a custom field is mapped.
                    ReorderPoint = 0m,
                    RecentSalesVolume = 0m,
                    IsLowStock = false
                };
                dbContext.InventoryItems.Add(created);
                existing[itemId] = created;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> SyncStockLocationsAsync(int pageSize, CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByFields(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.StockLocations(), pageSize, cancellationToken),
            "item",
            "location");
        // Stock rows use an identity PK, so match the business key (item + location) to avoid duplicates.
        var existing = (await dbContext.StockLocations.ToListAsync(cancellationToken))
            .ToDictionary(
                location => StockLocationKey(location.ItemId, location.LocationId),
                StringComparer.OrdinalIgnoreCase);
        var imported = 0;
        foreach (var row in rows)
        {
            var itemId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "item");
            var locationId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "location");
            var locationName = NetSuiteSuiteQlRowReader.GetString(row, "locationname")
                ?? NetSuiteSuiteQlRowReader.GetString(row, "location")
                ?? "Unknown location";
            var quantityOnHand = NetSuiteSuiteQlRowReader.GetDecimal(row, "quantity", "quantityonhand");
            var key = StockLocationKey(itemId, locationId);

            if (existing.TryGetValue(key, out var location))
            {
                location.LocationName = locationName;
                location.QuantityOnHand = quantityOnHand;
            }
            else
            {
                var created = new StockLocationRecord
                {
                    ItemId = itemId,
                    LocationId = locationId,
                    LocationName = locationName,
                    QuantityOnHand = quantityOnHand
                };
                dbContext.StockLocations.Add(created);
                existing[key] = created;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> SyncRevenueAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var customerNames = await dbContext.Customers
            .AsNoTracking()
            .ToDictionaryAsync(customer => customer.CustomerId, customer => customer.Name, cancellationToken);

        var rows = NetSuiteSuiteQlRowDeduplicator.ByFields(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Invoices(fromDate), pageSize, cancellationToken),
            "invoiceid",
            "lineid");
        // Historical invoice lines stay in the dashboard even after they fall outside the lookback window.
        var existingIds = (await dbContext.RevenueRecords
            .AsNoTracking()
            .Select(record => record.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var imported = 0;
        foreach (var row in rows)
        {
            // Skip instead of storing UTC today when tranDate is missing or still unparseable.
            if (!NetSuiteSuiteQlRowReader.TryGetDate(row, out var tranDate, "trandate", "tranDate"))
            {
                logger.LogWarning(
                    "Skipping invoice {InvoiceId}: tranDate '{TranDate}' is missing or not a real transaction date.",
                    NetSuiteSuiteQlRowReader.GetString(row, "invoiceid", "id"),
                    NetSuiteSuiteQlRowReader.GetString(row, "trandate", "tranDate") ?? "(missing)");
                continue;
            }

            var invoiceId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "invoiceid", "id");
            var lineId = NetSuiteSuiteQlRowReader.GetString(row, "lineid") ?? "0";
            var revenueId = $"{invoiceId}-{lineId}";
            // Only append lines we have not stored yet; do not rewrite older amounts or names.
            if (!existingIds.Add(revenueId))
            {
                continue;
            }

            var customerId = NetSuiteSuiteQlRowReader.GetString(row, "customerid") ?? string.Empty;
            dbContext.RevenueRecords.Add(new RevenueRecord
            {
                Id = revenueId,
                // Keep the parent invoice so the UI can group lines under one document.
                InvoiceId = invoiceId,
                InvoiceNumber = NetSuiteSuiteQlRowReader.GetString(row, "invoicenumber", "tranid", "tranId") ?? string.Empty,
                Date = tranDate,
                Amount = NetSuiteSuiteQlRowReader.GetDecimal(row, "amount"),
                CustomerId = customerId,
                CustomerName = customerNames.GetValueOrDefault(customerId) ?? string.Empty,
                ItemId = NetSuiteSuiteQlRowReader.GetString(row, "itemid", "item") ?? string.Empty,
                ItemName = NetSuiteSuiteQlRowReader.GetString(row, "itemname") ?? string.Empty,
                // Artikelgroep is item class, not subsidiary. Empty name is better than a raw id.
                ProductGroupId = NetSuiteSuiteQlRowReader.GetString(row, "productgroupid") ?? string.Empty,
                ProductGroupName = NetSuiteSuiteQlRowReader.GetString(row, "productgroupname", "productGroupName") ?? string.Empty
            });
            imported++;
        }

        return imported;
    }

    private async Task<int> SyncExpensesAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var supplierNames = await dbContext.Suppliers
            .AsNoTracking()
            .ToDictionaryAsync(supplier => supplier.SupplierId, supplier => supplier.Name, cancellationToken);

        var rows = NetSuiteSuiteQlRowDeduplicator.ByField(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.VendorBills(fromDate), pageSize, cancellationToken),
            "id");
        var existingIds = (await dbContext.Expenses
            .AsNoTracking()
            .Select(expense => expense.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var imported = 0;
        foreach (var row in rows)
        {
            if (!NetSuiteSuiteQlRowReader.TryGetDate(row, out var tranDate, "trandate", "tranDate"))
            {
                logger.LogWarning(
                    "Skipping vendor bill {BillId}: trandate '{TranDate}' is missing or not a real transaction date.",
                    NetSuiteSuiteQlRowReader.GetString(row, "id"),
                    NetSuiteSuiteQlRowReader.GetString(row, "trandate", "tranDate") ?? "(missing)");
                continue;
            }

            var expenseId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "id");
            // Keep older bills that are no longer in the lookback query.
            if (!existingIds.Add(expenseId))
            {
                continue;
            }

            var supplierId = NetSuiteSuiteQlRowReader.GetString(row, "supplierid");
            dbContext.Expenses.Add(new ExpenseRecord
            {
                Id = expenseId,
                Date = tranDate,
                Amount = NetSuiteSuiteQlRowReader.GetDecimal(row, "amount"),
                Category = "Vendor Bill",
                VendorName = supplierId is null
                    ? string.Empty
                    : supplierNames.GetValueOrDefault(supplierId) ?? string.Empty,
                SupplierId = supplierId
            });
            imported++;
        }

        return imported;
    }

    private async Task<int> SyncOpenTransactionsAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var existing = await dbContext.OpenTransactions.ToDictionaryAsync(
            transaction => transaction.TransactionId,
            cancellationToken);

        var salesRows = NetSuiteSuiteQlRowDeduplicator.ByFields(
            await restClient.QuerySuiteQlAsync(
                NetSuiteSuiteQlQueries.OpenSalesOrderLines(fromDate),
                pageSize,
                cancellationToken),
            "transaction",
            "lineid");

        var imported = 0;
        foreach (var row in salesRows)
        {
            var mapped = MapOpenTransactionRow(
                row,
                type: "Sales Order",
                customerId: NetSuiteSuiteQlRowReader.GetString(row, "customerid"),
                supplierId: null);
            if (mapped is null)
            {
                continue;
            }

            imported += UpsertOpenTransaction(existing, mapped);
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
            var mapped = MapOpenTransactionRow(
                row,
                type: "Purchase Order",
                customerId: null,
                supplierId: NetSuiteSuiteQlRowReader.GetString(row, "supplierid"));
            if (mapped is null)
            {
                continue;
            }

            imported += UpsertOpenTransaction(existing, mapped);
        }

        return imported;
    }

    /// <summary>
    /// Adds a new open-order line or refreshes quantity/status on an existing one. Closed orders
    /// that NetSuite no longer returns are left in place so history is not wiped.
    /// </summary>
    private int UpsertOpenTransaction(
        Dictionary<string, OpenTransactionRecord> existing,
        OpenTransactionRecord mapped)
    {
        if (existing.TryGetValue(mapped.TransactionId, out var stored))
        {
            stored.Type = mapped.Type;
            stored.ItemId = mapped.ItemId;
            stored.ItemName = mapped.ItemName;
            stored.CustomerId = mapped.CustomerId;
            stored.SupplierId = mapped.SupplierId;
            stored.TransactionDate = mapped.TransactionDate;
            stored.QuantityOpen = mapped.QuantityOpen;
            stored.Amount = mapped.Amount;
            stored.Status = mapped.Status;
            return 1;
        }

        dbContext.OpenTransactions.Add(mapped);
        existing[mapped.TransactionId] = mapped;
        return 1;
    }

    private OpenTransactionRecord? MapOpenTransactionRow(
        IReadOnlyDictionary<string, JsonElement> row,
        string type,
        string? customerId,
        string? supplierId)
    {
        // Composite key keeps line-level open orders unique in the dashboard table.
        var transactionId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "transaction");
        var lineId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "lineid");

        // Same rule as invoices: a bad tranDate must not become "today".
        if (!NetSuiteSuiteQlRowReader.TryGetDate(row, out var transactionDate, "trandate", "tranDate"))
        {
            logger.LogWarning(
                "Skipping open {Type} {TransactionId}-{LineId}: tranDate '{TranDate}' is missing or not a real transaction date.",
                type,
                transactionId,
                lineId,
                NetSuiteSuiteQlRowReader.GetString(row, "trandate", "tranDate") ?? "(missing)");
            return null;
        }

        return new OpenTransactionRecord
        {
            TransactionId = $"{transactionId}-{lineId}",
            Type = type,
            ItemId = NetSuiteSuiteQlRowReader.GetString(row, "itemid") ?? string.Empty,
            ItemName = NetSuiteSuiteQlRowReader.GetString(row, "itemname") ?? string.Empty,
            CustomerId = customerId,
            SupplierId = supplierId,
            TransactionDate = transactionDate,
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

    private static string StockLocationKey(string itemId, string locationId) =>
        $"{itemId}|{locationId}";

    /// <summary>
    /// Keeps a list-field label and hides a bare numeric NetSuite internal id.
    /// </summary>
    private static string ListFieldDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.All(char.IsDigit) ? string.Empty : value;
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
