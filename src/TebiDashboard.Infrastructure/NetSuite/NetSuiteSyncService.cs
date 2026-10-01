using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.Data.Entities;
using TebiDashboard.Shared.Constants;

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
    ILogger<NetSuiteSyncService> logger,
    INetSuiteSyncCoordinator syncCoordinator) : INetSuiteSyncService
{
    public async Task<NetSuiteSyncResult> SyncDashboardDataAsync(int lookbackDays, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            return Failed(settings.DescribeMissingConfiguration()
                + " Set NetSuite environment variables on the API service (e.g. NetSuite__Enabled=true).");
        }

        var days = NetSuiteLookback.Clamp(lookbackDays);
        var lookbackFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
        var pageSize = settings.SyncPageSize;
        var progress = new SyncStepProgress
        {
            Completed = 0,
            Total = settings.ClearDashboardDataOnSync ? 9 : 8
        };
        // Publish the step count immediately so the admin bar is determinate from the first poll.
        syncCoordinator.ReportProgress("starting", progress.Completed, progress.Total);

        try
        {
            // Full replace is opt-in. Normal syncs keep stored rows and only add or refresh what NetSuite returns.
            if (settings.ClearDashboardDataOnSync)
            {
                await RunStepAsync("clear dashboard tables", () => ClearDashboardTablesAsync(cancellationToken), progress);
            }

            var customers = await RunStepAsync("customers", () => SyncCustomersAsync(pageSize, cancellationToken), progress);
            await dbContext.SaveChangesAsync(cancellationToken);

            var suppliers = await RunStepAsync("suppliers", () => SyncSuppliersAsync(pageSize, cancellationToken), progress);
            await dbContext.SaveChangesAsync(cancellationToken);

            var inventoryItems = await RunStepAsync("inventory items", () => SyncInventoryItemsAsync(pageSize, cancellationToken), progress);
            await dbContext.SaveChangesAsync(cancellationToken);

            var stockLocations = await RunStepAsync("stock locations", () => SyncStockLocationsAsync(pageSize, cancellationToken), progress);
            // Persist stock first so the on-hand rollup reads the rows just imported or refreshed.
            await dbContext.SaveChangesAsync(cancellationToken);
            await ApplyInventoryQuantitiesFromStockLocationsAsync(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var revenue = await RunStepAsync("invoices", () => SyncRevenueAsync(lookbackFrom, pageSize, cancellationToken), progress);
            await dbContext.SaveChangesAsync(cancellationToken);

            var expenses = await RunStepAsync("vendor bills", () => SyncExpensesAsync(lookbackFrom, pageSize, cancellationToken), progress);
            await dbContext.SaveChangesAsync(cancellationToken);

            var openTransactions = await RunStepAsync(
                "open sales and purchase orders",
                () => SyncOpenTransactionsAsync(lookbackFrom, pageSize, cancellationToken),
                progress);
            await dbContext.SaveChangesAsync(cancellationToken);

            await RunStepAsync(
                "recent item sales",
                () => ApplyRecentSalesVolumeAsync(lookbackFrom, pageSize, cancellationToken),
                progress);
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

    private async Task<int> RunStepAsync(
        string stepName,
        Func<Task<int>> step,
        SyncStepProgress progress)
    {
        try
        {
            logger.LogInformation("NetSuite sync step started: {StepName}", stepName);
            syncCoordinator.ReportProgress(stepName, progress.Completed, progress.Total);
            var count = await step();
            progress.Completed++;
            syncCoordinator.ReportProgress(stepName, progress.Completed, progress.Total);
            logger.LogInformation("NetSuite sync step completed: {StepName} ({Count} rows)", stepName, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NetSuite sync step failed: {StepName}", stepName);
            throw new InvalidOperationException($"Sync step '{stepName}' failed: {ex.Message}", ex);
        }
    }

    private async Task RunStepAsync(
        string stepName,
        Func<Task> step,
        SyncStepProgress progress)
    {
        try
        {
            logger.LogInformation("NetSuite sync step started: {StepName}", stepName);
            syncCoordinator.ReportProgress(stepName, progress.Completed, progress.Total);
            await step();
            progress.Completed++;
            syncCoordinator.ReportProgress(stepName, progress.Completed, progress.Total);
            logger.LogInformation("NetSuite sync step completed: {StepName}", stepName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NetSuite sync step failed: {StepName}", stepName);
            throw new InvalidOperationException($"Sync step '{stepName}' failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Mutable counters for one sync run. Async methods cannot take ref parameters.
    /// </summary>
    private sealed class SyncStepProgress
    {
        public int Completed { get; set; }

        public int Total { get; init; }
    }

    private async Task ClearDashboardTablesAsync(CancellationToken cancellationToken)
    {
        // Keep users, goals, and the last-sync checkpoint; replace imported dashboard facts.
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
            // entityId is the K###### code; append glommedName (person) or companyName (company).
            var name = CustomerDisplayName.From(
                NetSuiteSuiteQlRowReader.GetString(row, "entityid", "entityId"),
                NetSuiteSuiteQlRowReader.GetString(row, "isperson", "isPerson"),
                NetSuiteSuiteQlRowReader.GetString(row, "glommedname", "glommedName"),
                NetSuiteSuiteQlRowReader.GetString(row, "companyname", "companyName"));
            // City comes from the default billing address-book row, not the full address blob.
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
                    // Internal id matches invoice.entity; Name is entityId plus the type-specific name.
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
            // entityId is the vendor code; companyName is the company, same pairing as customers.
            var name = SupplierDisplayName.From(
                NetSuiteSuiteQlRowReader.GetString(row, "entityid", "entityId"),
                NetSuiteSuiteQlRowReader.GetString(row, "companyname", "companyName"));
            // Same city mapping as customers: default billing address-book city.
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
                    // Internal id matches vendor bill entity. Name is entityId plus companyName.
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
                    // Item-header reorderPoint is not used; location configs fill this after stock sync.
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
            // Never store the internal location id as the name; the UI would then show "16".
            var incomingName = ListFieldDisplayName(NetSuiteSuiteQlRowReader.GetString(row, "locationname"));
            var quantityOnHand = NetSuiteSuiteQlRowReader.GetDecimal(row, "quantity", "quantityonhand");
            var key = StockLocationKey(itemId, locationId);

            if (existing.TryGetValue(key, out var location))
            {
                if (!string.IsNullOrEmpty(incomingName))
                {
                    location.LocationName = incomingName;
                }
                else if (location.LocationName.All(char.IsDigit))
                {
                    location.LocationName = string.Empty;
                }

                location.QuantityOnHand = quantityOnHand;
            }
            else
            {
                var created = new StockLocationRecord
                {
                    ItemId = itemId,
                    LocationId = locationId,
                    LocationName = incomingName,
                    QuantityOnHand = quantityOnHand
                };
                dbContext.StockLocations.Add(created);
                existing[key] = created;
            }

            imported++;
        }

        // Reorder point lives on itemlocationconfiguration (per item + location), not on SuiteQL item.
        imported += await ApplyItemLocationConfigurationsAsync(pageSize, existing, cancellationToken);

        return imported;
    }

    private async Task<int> ApplyItemLocationConfigurationsAsync(
        int pageSize,
        Dictionary<string, StockLocationRecord> existing,
        CancellationToken cancellationToken)
    {
        var rows = NetSuiteSuiteQlRowDeduplicator.ByFields(
            await restClient.QuerySuiteQlAsync(
                NetSuiteSuiteQlQueries.ItemLocationConfigurations(),
                pageSize,
                cancellationToken),
            "item",
            "location");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var imported = 0;
        foreach (var row in rows)
        {
            var itemId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "item");
            var locationId = NetSuiteSuiteQlRowReader.GetRequiredString(row, "location");
            var incomingName = ListFieldDisplayName(NetSuiteSuiteQlRowReader.GetString(row, "locationname"));
            var reorderPoint = NetSuiteSuiteQlRowReader.GetDecimal(row, "reorderpoint");
            var preferredStockLevel = NetSuiteSuiteQlRowReader.GetDecimal(row, "preferredstocklevel");
            var key = StockLocationKey(itemId, locationId);
            seen.Add(key);

            if (existing.TryGetValue(key, out var location))
            {
                if (!string.IsNullOrEmpty(incomingName))
                {
                    location.LocationName = incomingName;
                }
                else if (location.LocationName.All(char.IsDigit))
                {
                    location.LocationName = string.Empty;
                }

                location.ReorderPoint = reorderPoint;
                location.PreferredStockLevel = preferredStockLevel;
            }
            else
            {
                // Config can exist for a location with zero on-hand; still needed for low-stock.
                var created = new StockLocationRecord
                {
                    ItemId = itemId,
                    LocationId = locationId,
                    LocationName = incomingName,
                    QuantityOnHand = 0m,
                    ReorderPoint = reorderPoint,
                    PreferredStockLevel = preferredStockLevel
                };
                dbContext.StockLocations.Add(created);
                existing[key] = created;
            }

            imported++;
        }

        foreach (var location in existing.Values)
        {
            if (!seen.Contains(StockLocationKey(location.ItemId, location.LocationId)))
            {
                location.ReorderPoint = 0m;
                location.PreferredStockLevel = 0m;
            }
        }

        return imported;
    }

    private async Task<int> SyncRevenueAsync(DateOnly fromDate, int pageSize, CancellationToken cancellationToken)
    {
        var customerNames = await dbContext.Customers
            .AsNoTracking()
            .ToDictionaryAsync(customer => customer.CustomerId, customer => customer.Name, cancellationToken);

        // Historical invoice lines stay in the dashboard even after they fall outside the lookback window.
        // Lines imported before units were stored still have Quantity 0. Read those dates too, once.
        var quantityFrom = await EarliestUncapturedInvoiceDateAsync(fromDate, cancellationToken);
        if (quantityFrom < fromDate)
        {
            logger.LogInformation(
                "Invoice units are missing before {LookbackFrom}. Reading invoices from {QuantityFrom}.",
                fromDate,
                quantityFrom);
        }

        var rows = NetSuiteSuiteQlRowDeduplicator.ByFields(
            await restClient.QuerySuiteQlAsync(NetSuiteSuiteQlQueries.Invoices(quantityFrom), pageSize, cancellationToken),
            "invoiceid",
            "lineid");
        var existingIds = (await dbContext.RevenueRecords
            .AsNoTracking()
            .Select(record => record.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var quantityByExistingId = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var imported = 0;
        var sawQuantityColumn = false;
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
            // soldquantity is NVL(quantity, 0) * -1. A missing column must not be stored as zero.
            var hasQuantity = NetSuiteSuiteQlRowReader.HasDecimal(row, "soldquantity", "quantity");
            if (hasQuantity)
            {
                sawQuantityColumn = true;
            }

            var quantity = hasQuantity
                ? NetSuiteSuiteQlRowReader.GetDecimal(row, "soldquantity", "quantity")
                : 0m;
            // Lines imported before quantity was stored still need it filled on the next sync.
            if (!existingIds.Add(revenueId))
            {
                if (hasQuantity)
                {
                    quantityByExistingId[revenueId] = quantity;
                }

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
                ProductGroupName = NetSuiteSuiteQlRowReader.GetString(row, "productgroupname", "productGroupName") ?? string.Empty,
                Quantity = quantity,
                UnitsCaptured = hasQuantity
            });
            imported++;
        }

        await ApplyInvoiceQuantitiesAsync(quantityByExistingId, cancellationToken);
        if (sawQuantityColumn || rows.Count == 0)
        {
            await MarkUncapturedInvoiceLinesAsync(quantityFrom, cancellationToken);
        }
        else
        {
            logger.LogWarning("SuiteQL invoice rows did not include soldquantity. Stored units were left unchanged.");
        }

        // Incremental sync skips existing keys, so older credit-signed amounts stay until flipped here.
        imported += await FlipNegativeRevenueAmountsAsync(cancellationToken);
        await FlipNegativeRevenueQuantitiesAsync(cancellationToken);

        return imported;
    }

    /// <summary>
    /// Start of the invoice read. Uses the lookback unless older item lines still have no units.
    /// </summary>
    private async Task<DateOnly> EarliestUncapturedInvoiceDateAsync(
        DateOnly lookbackFrom,
        CancellationToken cancellationToken)
    {
        var earliest = await dbContext.RevenueRecords
            .AsNoTracking()
            .Where(record => !record.UnitsCaptured && record.ItemId != string.Empty)
            .Select(record => (DateOnly?)record.Date)
            .MinAsync(cancellationToken);

        return earliest is DateOnly gap && gap < lookbackFrom ? gap : lookbackFrom;
    }

    /// <summary>
    /// Writes units onto invoice lines that were stored before quantity was selected.
    /// </summary>
    private async Task ApplyInvoiceQuantitiesAsync(
        Dictionary<string, decimal> quantityById,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in quantityById.Keys.Chunk(500))
        {
            var ids = chunk.ToList();
            var records = await dbContext.RevenueRecords
                .Where(record => ids.Contains(record.Id))
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                if (quantityById.TryGetValue(record.Id, out var quantity))
                {
                    record.Quantity = quantity;
                    record.UnitsCaptured = true;
                }
            }
        }
    }

    /// <summary>
    /// Lines in the queried range that NetSuite no longer returns are marked captured
    /// so the next sync does not download that history again.
    /// </summary>
    private async Task MarkUncapturedInvoiceLinesAsync(DateOnly quantityFrom, CancellationToken cancellationToken)
    {
        var leftovers = await dbContext.RevenueRecords
            .Where(record => !record.UnitsCaptured && record.ItemId != string.Empty && record.Date >= quantityFrom)
            .ToListAsync(cancellationToken);

        foreach (var record in leftovers)
        {
            record.UnitsCaptured = true;
        }
    }

    /// <summary>
    /// Invoice line quantity is negative when the line reduces inventory. Store units sold as positive.
    /// </summary>
    private async Task FlipNegativeRevenueQuantitiesAsync(CancellationToken cancellationToken)
    {
        var negativeRows = await dbContext.RevenueRecords
            .Where(record => record.Quantity < 0m)
            .ToListAsync(cancellationToken);

        foreach (var record in negativeRows)
        {
            record.Quantity = -record.Quantity;
        }
    }

    /// <summary>
    /// Turns stored invoice credits into positive omzet without rewriting names or dates.
    /// </summary>
    private async Task<int> FlipNegativeRevenueAmountsAsync(CancellationToken cancellationToken)
    {
        var negativeRows = await dbContext.RevenueRecords
            .Where(record => record.Amount < 0m)
            .ToListAsync(cancellationToken);

        foreach (var record in negativeRows)
        {
            record.Amount = -record.Amount;
        }

        return negativeRows.Count;
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
                NetSuiteSuiteQlQueries.OpenSalesOrderLines(),
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
        var locations = await dbContext.StockLocations.ToListAsync(cancellationToken);
        var quantitiesByItem = locations
            .GroupBy(location => location.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(location => location.QuantityOnHand),
                StringComparer.OrdinalIgnoreCase);
        var reorderByItem = locations
            .GroupBy(location => location.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(location => location.ReorderPoint),
                StringComparer.OrdinalIgnoreCase);
        var lowStockByItem = locations
            .GroupBy(location => location.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Any(location => location.ReorderPoint > 0 && location.QuantityOnHand <= location.ReorderPoint),
                StringComparer.OrdinalIgnoreCase);

        var items = await dbContext.InventoryItems.ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            var quantityOnHand = quantitiesByItem.GetValueOrDefault(item.ItemId);
            item.QuantityOnHand = quantityOnHand;
            // Item KPI shows the sum of location reorder points; the badge uses any-location low stock.
            item.ReorderPoint = reorderByItem.GetValueOrDefault(item.ItemId);
            item.IsLowStock = lowStockByItem.GetValueOrDefault(item.ItemId);
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
