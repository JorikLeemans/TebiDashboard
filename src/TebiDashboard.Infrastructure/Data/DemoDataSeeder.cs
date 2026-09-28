using Microsoft.EntityFrameworkCore;
using TebiDashboard.Infrastructure.Data.Entities;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Periods;

namespace TebiDashboard.Infrastructure.Data;

/// <summary>
/// Seeds users and dashboard demo datasets for local and hosted testing.
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>
    /// Inserts demo data when tables are empty, or when a full reseed is requested.
    /// </summary>
    public static async Task SeedAsync(TebiDashboardDbContext dbContext, bool forceReseed = false, CancellationToken cancellationToken = default)
    {
        if (forceReseed)
        {
            await ClearDashboardDataAsync(dbContext, cancellationToken);
        }

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            dbContext.Users.AddRange(
                new UserEntity { Email = "admin@tebi.nl", Password = "admin1234", Role = Roles.Admin },
                new UserEntity { Email = "finance@tebi.nl", Password = "finance123", Role = Roles.Finance },
                new UserEntity { Email = "sales@tebi.nl", Password = "sales1234", Role = Roles.Sales },
                new UserEntity { Email = "inventory@tebi.nl", Password = "inventory1", Role = Roles.Inventory });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Backfill master data when tables were added after an existing deployment.
        await EnsureDemoCustomersAsync(dbContext, cancellationToken);
        await BackfillSalesOrderCustomersAsync(dbContext, cancellationToken);
        await EnsureDemoSuppliersAsync(dbContext, cancellationToken);
        await BackfillPurchaseOrderSuppliersAsync(dbContext, cancellationToken);
        await BackfillExpenseSuppliersAsync(dbContext, cancellationToken);
        await EnsureDemoGoalsForYearAsync(dbContext, today, today.Year - 1, cancellationToken);
        await EnsureDemoRevenueSpreadAsync(dbContext, today, cancellationToken);
        await EnsureDemoInvoiceGroupingLinesAsync(dbContext, today, cancellationToken);
        await EnsureDemoSalesOrderSpreadAsync(dbContext, today, cancellationToken);
        await EnsureDemoSalesOrderGroupingLinesAsync(dbContext, today, cancellationToken);
        await EnsureDemoExpenseSpreadAsync(dbContext, today, cancellationToken);
        await EnsureDemoPurchaseOrderSpreadAsync(dbContext, today, cancellationToken);
        await EnsureDemoPurchaseOrderGroupingLinesAsync(dbContext, today, cancellationToken);
        await EnsureDemoOrderAmountAlignmentAsync(dbContext, today, cancellationToken);
        // Spread rows are inserted before the full seed. Without this, transactions point at
        // item ids that were never inserted, and detail pages show "i5" instead of the item name.
        await EnsureDemoInventoryItemsAsync(dbContext, cancellationToken);
        // Existing databases were seeded with only the product-group id. Fill the name so the
        // running app shows "Bestrating" instead of "pg-paving" without a full reseed.
        await EnsureRevenueProductGroupNamesAsync(dbContext, cancellationToken);
        await EnsureRevenueAndTransactionItemNamesAsync(dbContext, cancellationToken);

        if (await dbContext.RevenueRecords.AnyAsync(cancellationToken))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var year = today.Year;

        dbContext.RevenueRecords.AddRange(BuildDemoRevenueRecords(today));

        dbContext.Goals.AddRange(BuildDemoGoals(today, year));
        dbContext.Goals.AddRange(BuildDemoGoals(today, year - 1));

        dbContext.InventoryItems.AddRange(GetDemoInventoryItems());

        dbContext.StockLocations.AddRange(
            CreateLocation("i1", "loc1", "Hoofdmagazijn", 300),
            CreateLocation("i1", "loc2", "Terrein A", 150),
            CreateLocation("i2", "loc1", "Hoofdmagazijn", 22),
            CreateLocation("i3", "loc1", "Hoofdmagazijn", 140),
            CreateLocation("i3", "loc3", "Terrein B", 70),
            CreateLocation("i4", "loc2", "Terrein A", 38),
            CreateLocation("i5", "loc1", "Hoofdmagazijn", 520),
            CreateLocation("i6", "loc4", "Bulkopslag", 18),
            CreateLocation("i9", "loc1", "Hoofdmagazijn", 12));

        dbContext.OpenTransactions.AddRange(BuildDemoOpenTransactions(today));

        dbContext.Expenses.AddRange(
            CreateExpense("e1", today.AddDays(-80), "Transport", 12500m, "Logistics Partner BV", "s3"),
            CreateExpense("e2", today.AddDays(-65), "Utilities", 4200m, "Energiebedrijf NL", "s4"),
            CreateExpense("e3", today.AddDays(-50), "Maintenance", 6800m, "Technische Dienst BV", "s5"),
            CreateExpense("e4", today.AddDays(-40), "Transport", 9800m, "Logistics Partner BV", "s3"),
            CreateExpense("e5", today.AddDays(-28), "Marketing", 3500m, "Media Studio Utrecht", "s7"),
            CreateExpense("e6", today.AddDays(-18), "Utilities", 4100m, "Energiebedrijf NL", "s4"),
            CreateExpense("e7", today.AddDays(-12), "Insurance", 7200m, "Verzekeraar NL", "s8"),
            CreateExpense("e8", today.AddDays(-5), "Maintenance", 2950m, "Technische Dienst BV", "s5"));
        dbContext.Expenses.AddRange(BuildDemoExpenseSpread(today));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Removes dashboard rows so demo data can be reloaded without touching registered users.
    /// </summary>
    public static async Task ClearDashboardDataAsync(TebiDashboardDbContext dbContext, CancellationToken cancellationToken = default)
    {
        dbContext.OpenTransactions.RemoveRange(dbContext.OpenTransactions);
        dbContext.StockLocations.RemoveRange(dbContext.StockLocations);
        dbContext.Expenses.RemoveRange(dbContext.Expenses);
        dbContext.Goals.RemoveRange(dbContext.Goals);
        dbContext.RevenueRecords.RemoveRange(dbContext.RevenueRecords);
        dbContext.Customers.RemoveRange(dbContext.Customers);
        dbContext.Suppliers.RemoveRange(dbContext.Suppliers);
        dbContext.InventoryItems.RemoveRange(dbContext.InventoryItems);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds smaller revenue lines on separate days so charts show visible variation.
    /// </summary>
    private static async Task EnsureDemoRevenueSpreadAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (await dbContext.RevenueRecords.AnyAsync(revenue => revenue.Id == "r16", cancellationToken))
        {
            return;
        }

        dbContext.RevenueRecords.AddRange(BuildDemoRevenueSpread(today));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds extra sales orders spread across recent days for transaction charts.
    /// </summary>
    private static async Task EnsureDemoSalesOrderSpreadAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (await dbContext.OpenTransactions.AnyAsync(transaction => transaction.TransactionId == "t7", cancellationToken))
        {
            return;
        }

        dbContext.OpenTransactions.AddRange(BuildDemoSalesOrderSpread(today));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RevenueRecord[] BuildDemoRevenueRecords(DateOnly today) =>
    [
        CreateRevenue("r01", today.AddDays(-165), 38500m, "c1", "Bouwbedrijf Jansen", "pg-paving"),
        CreateRevenue("r02", today.AddDays(-150), 41200m, "c2", "Gemeente Utrecht", "pg-tiles"),
        CreateRevenue("r03", today.AddDays(-135), 29800m, "c3", "Stratenmakers BV", "pg-paving"),
        CreateRevenue("r04", today.AddDays(-120), 52100m, "c4", "Woningbouw De Vliet", "pg-paving"),
        CreateRevenue("r05", today.AddDays(-105), 36750m, "c1", "Bouwbedrijf Jansen", "pg-edging"),
        CreateRevenue("r06", today.AddDays(-90), 44300m, "c5", "Aannemer Noord", "pg-tiles"),
        CreateRevenue("r07", today.AddDays(-75), 48900m, "c2", "Gemeente Utrecht", "pg-paving"),
        CreateRevenue("r08", today.AddDays(-60), 33600m, "c3", "Stratenmakers BV", "pg-edging"),
        CreateRevenue("r09", today.AddDays(-45), 55400m, "c4", "Woningbouw De Vliet", "pg-tiles"),
        CreateRevenue("r10", today.AddDays(-30), 42000m, "c1", "Bouwbedrijf Jansen", "pg-paving"),
        CreateRevenue("r11", today.AddDays(-21), 38500m, "c2", "Gemeente Utrecht", "pg-tiles"),
        CreateRevenue("r12", today.AddDays(-14), 51200m, "c3", "Stratenmakers BV", "pg-paving"),
        CreateRevenue("r13", today.AddDays(-10), 27800m, "c5", "Aannemer Noord", "pg-edging"),
        CreateRevenue("r14", today.AddDays(-7), 46100m, "c4", "Woningbouw De Vliet", "pg-paving"),
        CreateRevenue("r15", today.AddDays(-3), 39500m, "c1", "Bouwbedrijf Jansen", "pg-tiles"),
        ..BuildDemoRevenueSpread(today),
        ..BuildDemoInvoiceGroupingLines(today)
    ];

    private static RevenueRecord[] BuildDemoRevenueSpread(DateOnly today) =>
    [
        CreateRevenue("r16", today.AddDays(-44), 8200m, "c2", "Gemeente Utrecht", "pg-tiles"),
        CreateRevenue("r17", today.AddDays(-41), 15600m, "c3", "Stratenmakers BV", "pg-paving"),
        CreateRevenue("r18", today.AddDays(-39), 6800m, "c1", "Bouwbedrijf Jansen", "pg-edging"),
        CreateRevenue("r19", today.AddDays(-37), 22400m, "c4", "Woningbouw De Vliet", "pg-paving"),
        CreateRevenue("r20", today.AddDays(-35), 9400m, "c5", "Aannemer Noord", "pg-tiles"),
        CreateRevenue("r21", today.AddDays(-33), 18700m, "c2", "Gemeente Utrecht", "pg-paving"),
        CreateRevenue("r22", today.AddDays(-32), 5200m, "c3", "Stratenmakers BV", "pg-edging"),
        CreateRevenue("r23", today.AddDays(-29), 14300m, "c1", "Bouwbedrijf Jansen", "pg-paving"),
        CreateRevenue("r24", today.AddDays(-27), 7600m, "c4", "Woningbouw De Vliet", "pg-tiles"),
        CreateRevenue("r25", today.AddDays(-25), 19800m, "c2", "Gemeente Utrecht", "pg-tiles"),
        CreateRevenue("r26", today.AddDays(-23), 11200m, "c5", "Aannemer Noord", "pg-paving"),
        CreateRevenue("r27", today.AddDays(-20), 6300m, "c3", "Stratenmakers BV", "pg-paving"),
        CreateRevenue("r28", today.AddDays(-19), 17400m, "c1", "Bouwbedrijf Jansen", "pg-tiles"),
        CreateRevenue("r29", today.AddDays(-17), 8900m, "c4", "Woningbouw De Vliet", "pg-edging"),
        CreateRevenue("r30", today.AddDays(-16), 23100m, "c2", "Gemeente Utrecht", "pg-paving"),
        CreateRevenue("r31", today.AddDays(-13), 7200m, "c5", "Aannemer Noord", "pg-tiles"),
        CreateRevenue("r32", today.AddDays(-11), 16500m, "c3", "Stratenmakers BV", "pg-tiles"),
        CreateRevenue("r33", today.AddDays(-9), 10800m, "c1", "Bouwbedrijf Jansen", "pg-paving"),
        CreateRevenue("r34", today.AddDays(-8), 5400m, "c4", "Woningbouw De Vliet", "pg-paving"),
        CreateRevenue("r35", today.AddDays(-6), 19200m, "c2", "Gemeente Utrecht", "pg-edging"),
        CreateRevenue("r36", today.AddDays(-4), 8700m, "c5", "Aannemer Noord", "pg-paving"),
        CreateRevenue("r37", today.AddDays(-2), 14900m, "c3", "Stratenmakers BV", "pg-paving"),
        CreateRevenue("r38", today.AddDays(-1), 6100m, "c1", "Bouwbedrijf Jansen", "pg-edging")
    ];

    private static OpenTransactionRecord[] BuildDemoOpenTransactions(DateOnly today) =>
    [
        CreateTransaction("t1", "Sales Order", "i1", today.AddDays(-1), 40, "Pending Fulfillment", customerId: "c1"),
        CreateTransaction("t2", "Transfer Order", "i1", today.AddDays(-2), 20, "Pending Receipt"),
        CreateTransaction("t3", "Sales Order", "i2", today.AddDays(-5), 55, "Pending Fulfillment", customerId: "c3"),
        CreateTransaction("t4", "Purchase Order", "i4", today.AddDays(-12), 120, "Pending Receipt", supplierId: "s1"),
        CreateTransaction("t5", "Sales Order", "i3", today.AddDays(-3), 30, "Pending Fulfillment", customerId: "c2"),
        CreateTransaction("t6", "Transfer Order", "i6", today.AddDays(-18), 45, "In Transit"),
        ..BuildDemoSalesOrderSpread(today),
        ..BuildDemoSalesOrderGroupingLines(today),
        ..BuildDemoPurchaseOrderSpread(today),
        ..BuildDemoPurchaseOrderGroupingLines(today)
    ];

    private static ExpenseRecord[] BuildDemoExpenseSpread(DateOnly today) =>
    [
        // Recent supplier expenses so the default "Last 2 days" filter shows spend.
        CreateExpense("e9", today.AddDays(-2), "Transport", 8400m, "Logistics Partner BV", "s3"),
        CreateExpense("e10", today.AddDays(-1), "Utilities", 5200m, "Energiebedrijf NL", "s4"),
        CreateExpense("e11", today, "Maintenance", 3600m, "Technische Dienst BV", "s5"),
        CreateExpense("e12", today.AddDays(-2), "Insurance", 4800m, "Verzekeraar NL", "s8")
    ];

    private static OpenTransactionRecord[] BuildDemoPurchaseOrderSpread(DateOnly today) =>
    [
        CreateTransaction("t15", "Purchase Order", "i2", today.AddDays(-20), 80, "Pending Receipt", supplierId: "s2"),
        CreateTransaction("t16", "Purchase Order", "i6", today.AddDays(-8), 200, "Pending Receipt", supplierId: "s1"),
        CreateTransaction("t17", "Purchase Order", "i3", today.AddDays(-25), 95, "Pending Receipt", supplierId: "s6"),
        CreateTransaction("t18", "Purchase Order", "i8", today.AddDays(-14), 60, "Pending Receipt", supplierId: "s6"),
        // Recent purchase orders linked to suppliers for the default period filter.
        CreateTransaction("t19", "Purchase Order", "i4", today.AddDays(-2), 45, "Pending Receipt", supplierId: "s1"),
        CreateTransaction("t20", "Purchase Order", "i2", today.AddDays(-1), 30, "Pending Receipt", supplierId: "s2"),
        CreateTransaction("t21", "Purchase Order", "i3", today, 55, "Pending Receipt", supplierId: "s6"),
        CreateTransaction("t22", "Purchase Order", "i8", today.AddDays(-1), 25, "Pending Receipt", supplierId: "s6")
    ];

    private static OpenTransactionRecord[] BuildDemoSalesOrderSpread(DateOnly today) =>
    [
        CreateTransaction("t7", "Sales Order", "i1", today.AddDays(-8), 25, "Pending Fulfillment", customerId: "c4"),
        CreateTransaction("t8", "Sales Order", "i5", today.AddDays(-11), 18, "Pending Fulfillment", customerId: "c1"),
        CreateTransaction("t9", "Sales Order", "i7", today.AddDays(-15), 32, "Pending Fulfillment", customerId: "c2"),
        CreateTransaction("t10", "Sales Order", "i3", today.AddDays(-20), 22, "Pending Fulfillment", customerId: "c5"),
        CreateTransaction("t11", "Sales Order", "i8", today.AddDays(-24), 14, "Pending Fulfillment", customerId: "c3"),
        CreateTransaction("t12", "Sales Order", "i10", today.AddDays(-28), 36, "Pending Fulfillment", customerId: "c4"),
        CreateTransaction("t13", "Sales Order", "i2", today.AddDays(-34), 28, "Pending Fulfillment", customerId: "c1"),
        CreateTransaction("t14", "Sales Order", "i5", today.AddDays(-40), 20, "Pending Fulfillment", customerId: "c2")
    ];

    /// <summary>
    /// Extra invoice lines for local grouping tests: extra rows on F-C1 plus distinct multi-line invoices.
    /// </summary>
    private static async Task EnsureDemoInvoiceGroupingLinesAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (await dbContext.RevenueRecords.AnyAsync(revenue => revenue.Id == "1842-1", cancellationToken))
        {
            return;
        }

        dbContext.RevenueRecords.AddRange(BuildDemoInvoiceGroupingLines(today));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Extra sales-order lines that reuse existing order prefixes and add a few new orders.
    /// </summary>
    private static async Task EnsureDemoSalesOrderGroupingLinesAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (await dbContext.OpenTransactions.AnyAsync(order => order.TransactionId == "t1-2", cancellationToken))
        {
            return;
        }

        dbContext.OpenTransactions.AddRange(BuildDemoSalesOrderGroupingLines(today));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Same invoice number with several item lines, plus extra lines on the original F-Cx invoices.
    private static RevenueRecord[] BuildDemoInvoiceGroupingLines(DateOnly today) =>
    [
        CreateRevenue("c1-39", today.AddDays(-1), 2450.50m, "c1", "Bouwbedrijf Jansen", "pg-paving"),
        CreateRevenue("c1-40", today.AddDays(-1), 890.25m, "c1", "Bouwbedrijf Jansen", "pg-tiles"),
        CreateRevenue("c2-39", today.AddDays(-2), 3100.00m, "c2", "Gemeente Utrecht", "pg-edging"),
        CreateRevenue("1842-1", today.AddDays(-2), 324.00m, "c1", "Bouwbedrijf Jansen", "pg-paving", "1842", "F-1842"),
        CreateRevenue("1842-2", today.AddDays(-2), 45.76m, "c1", "Bouwbedrijf Jansen", "pg-tiles", "1842", "F-1842"),
        CreateRevenue("1842-3", today.AddDays(-2), 984.94m, "c1", "Bouwbedrijf Jansen", "pg-edging", "1842", "F-1842"),
        CreateRevenue("1842-4", today.AddDays(-2), 178.14m, "c1", "Bouwbedrijf Jansen", "pg-paving", "1842", "F-1842"),
        CreateRevenue("1843-1", today.AddDays(-1), 1250.00m, "c1", "Bouwbedrijf Jansen", "pg-tiles", "1843", "F-1843"),
        CreateRevenue("1843-2", today.AddDays(-1), 640.50m, "c1", "Bouwbedrijf Jansen", "pg-paving", "1843", "F-1843"),
        CreateRevenue("2001-1", today.AddDays(-3), 2200.00m, "c2", "Gemeente Utrecht", "pg-tiles", "2001", "F-2001"),
        CreateRevenue("2001-2", today.AddDays(-3), 880.00m, "c2", "Gemeente Utrecht", "pg-paving", "2001", "F-2001"),
        CreateRevenue("2001-3", today.AddDays(-3), 410.00m, "c2", "Gemeente Utrecht", "pg-edging", "2001", "F-2001"),
        CreateRevenue("3001-1", today.AddDays(-1), 1575.25m, "c3", "Stratenmakers BV", "pg-paving", "3001", "F-3001"),
        CreateRevenue("3001-2", today.AddDays(-1), 620.00m, "c3", "Stratenmakers BV", "pg-edging", "3001", "F-3001"),
        CreateRevenue("4001-1", today, 950.00m, "c4", "Woningbouw De Vliet", "pg-tiles", "4001", "F-4001"),
        CreateRevenue("4001-2", today, 430.00m, "c4", "Woningbouw De Vliet", "pg-paving", "4001", "F-4001")
    ];

    // Extra lines on t1/t8/t3, plus new multi-line orders t23 and t25.
    private static OpenTransactionRecord[] BuildDemoSalesOrderGroupingLines(DateOnly today) =>
    [
        CreateTransaction("t1-2", "Sales Order", "i2", today.AddDays(-1), 12, "Pending Fulfillment", customerId: "c1", amount: 14400m),
        CreateTransaction("t1-3", "Sales Order", "i5", today.AddDays(-1), 8, "Pending Fulfillment", customerId: "c1", amount: 6240m),
        CreateTransaction("t8-2", "Sales Order", "i1", today.AddDays(-11), 10, "Pending Fulfillment", customerId: "c1", amount: 8500m),
        CreateTransaction("t3-2", "Sales Order", "i3", today.AddDays(-5), 15, "Pending Fulfillment", customerId: "c3", amount: 8100m),
        CreateTransaction("t23-1", "Sales Order", "i1", today.AddDays(-2), 20, "Pending Fulfillment", customerId: "c1", amount: 17000m),
        CreateTransaction("t23-2", "Sales Order", "i3", today.AddDays(-2), 14, "Pending Fulfillment", customerId: "c1", amount: 7560m),
        CreateTransaction("t24", "Sales Order", "i2", today, 18, "Pending Fulfillment", customerId: "c1", amount: 21600m),
        CreateTransaction("t25-1", "Sales Order", "i2", today.AddDays(-1), 9, "Pending Fulfillment", customerId: "c2", amount: 10800m),
        CreateTransaction("t25-2", "Sales Order", "i5", today.AddDays(-1), 6, "Pending Fulfillment", customerId: "c2", amount: 4680m)
    ];

    private static RevenueRecord CreateRevenue(
        string id,
        DateOnly date,
        decimal amount,
        string customerId,
        string customerName,
        string productGroupId,
        string? invoiceId = null,
        string? invoiceNumber = null)
    {
        // Persist the representative item so detail rows do not depend on a later lookup.
        var linkedItemId = MapProductGroupToItem(productGroupId);
        return new RevenueRecord
        {
            Id = id,
            Date = date,
            Amount = amount,
            CustomerId = customerId,
            CustomerName = customerName,
            ProductGroupId = productGroupId,
            ProductGroupName = ResolveProductGroupName(productGroupId) ?? string.Empty,
            ItemId = linkedItemId,
            ItemName = DemoItemName(linkedItemId),
            // Default: one invoice per customer. Grouping lines pass a shared invoice id/number.
            InvoiceId = invoiceId ?? customerId,
            InvoiceNumber = invoiceNumber ?? $"F-{customerId.ToUpperInvariant()}"
        };
    }

    private static string DemoItemName(string itemId) =>
        GetDemoInventoryItems().FirstOrDefault(item => item.ItemId == itemId)?.ItemName ?? itemId;

    /// <summary>
    /// Inserts demo inventory items that are missing.
    /// The revenue/order spread runs before the full seed and then returns early once any
    /// revenue exists, so a database can have transactions without item master rows.
    /// Detail pages resolve ItemId to these names and otherwise fall back to the id.
    /// </summary>
    private static async Task EnsureDemoInventoryItemsAsync(
        TebiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var demoItems = GetDemoInventoryItems();
        var existingIds = await dbContext.InventoryItems
            .Select(item => item.ItemId)
            .ToListAsync(cancellationToken);

        var missing = demoItems.Where(item => !existingIds.Contains(item.ItemId)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        dbContext.InventoryItems.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static InventoryItemRecord[] GetDemoInventoryItems() =>
    [
        CreateItem("i1", "Betonstraatstenen Antraciet", "8712345000011", 450, 100, 980, false),
        CreateItem("i2", "Klinkers Rood-Bruin", "8712345000028", 22, 80, 640, true),
        CreateItem("i3", "Tegels 60x60 Grijs", "8712345000035", 210, 120, 760, false),
        CreateItem("i4", "Betonblokken 40x20", "8712345000042", 38, 90, 410, true),
        CreateItem("i5", "Opsluitbanden Grijs", "8712345000059", 520, 150, 320, false),
        CreateItem("i6", "Grind 16-32 mm", "8712345000066", 18, 60, 280, true),
        CreateItem("i7", "Zandstraatstenen Geel", "8712345000073", 175, 100, 540, false),
        CreateItem("i8", "Keramische Tegels Antraciet", "8712345000080", 95, 70, 490, false),
        CreateItem("i9", "Drainage Tegels", "8712345000097", 12, 50, 210, true),
        CreateItem("i10", "Beton Tegels 50x50", "8712345000103", 340, 110, 670, false)
    ];

    /// <summary>
    /// Demo artikelgroep names. The id stays on the record; the name is what the UI shows.
    /// Unknown ids return null so the UI can fall back to the raw id.
    /// </summary>
    private static string? ResolveProductGroupName(string productGroupId) => productGroupId switch
    {
        "pg-paving" or "pg1" => "Bestrating",
        "pg-tiles" or "pg2" => "Tegels",
        "pg-edging" or "pg3" => "Opsluitbanden",
        _ => null
    };

    /// <summary>
    /// Backfills product group names for revenue rows created before the name column existed.
    /// </summary>
    private static async Task EnsureRevenueProductGroupNamesAsync(
        TebiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.RevenueRecords
            .Where(revenue => revenue.ProductGroupName == null || revenue.ProductGroupName == "")
            .ToListAsync(cancellationToken);

        var updated = false;
        foreach (var revenue in rows)
        {
            var name = ResolveProductGroupName(revenue.ProductGroupId);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            revenue.ProductGroupName = name;
            updated = true;
        }

        if (updated)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Backfills item names on existing demo revenue and open orders so a restarted API
    /// stops showing raw ids without a full reseed.
    /// </summary>
    private static async Task EnsureRevenueAndTransactionItemNamesAsync(
        TebiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var namesById = GetDemoInventoryItems().ToDictionary(item => item.ItemId, item => item.ItemName);

        var revenueRows = await dbContext.RevenueRecords
            .Where(revenue => revenue.ItemName == null || revenue.ItemName == "")
            .ToListAsync(cancellationToken);
        foreach (var revenue in revenueRows)
        {
            // Only remap known demo product-group codes. A numeric NetSuite subsidiary id
            // such as "16" must not be rewritten to i1 / Betonstraatstenen.
            var itemId = string.IsNullOrWhiteSpace(revenue.ItemId)
                ? RevenueItemLink.TryGetItemId(revenue.ProductGroupId)
                : revenue.ItemId;
            if (string.IsNullOrWhiteSpace(itemId) || !namesById.TryGetValue(itemId, out var itemName))
            {
                continue;
            }

            revenue.ItemId = itemId;
            revenue.ItemName = itemName;
        }

        var orderRows = await dbContext.OpenTransactions
            .Where(order => order.ItemName == null || order.ItemName == "")
            .ToListAsync(cancellationToken);
        foreach (var order in orderRows)
        {
            if (namesById.TryGetValue(order.ItemId, out var itemName))
            {
                order.ItemName = itemName;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts prior-year goals when missing so year-over-year comparison works on existing databases.
    /// </summary>
    private static async Task EnsureDemoGoalsForYearAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        int year,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Goals.AnyAsync(goal => goal.Year == year, cancellationToken))
        {
            return;
        }

        dbContext.Goals.AddRange(BuildDemoGoals(today, year));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Demo goals for one calendar year (company + item targets per quarter).
    /// </summary>
    private static GoalRecord[] BuildDemoGoals(DateOnly today, int year)
    {
        if (year == today.Year - 1)
        {
            return
            [
                CreateGoal($"g-{year}-q1", today, year, "Q1", 460_000m, 471_000m, "on-track"),
                CreateGoal($"g-{year}-q2", today, year, "Q2", 480_000m, 462_000m, "on-track"),
                CreateGoal($"g-{year}-q3", today, year, "Q3", 490_000m, 505_000m, "ahead"),
                CreateGoal($"g-{year}-q4", today, year, "Q4", 510_000m, 498_000m, "on-track"),
                CreateGoal($"g-{year}-i1-q2", today, year, "Q2", 1150m, 1050m, "on-track", "i1", "Betonstraatstenen Antraciet"),
                CreateGoal($"g-{year}-i2-q2", today, year, "Q2", 850m, 720m, "behind", "i2", "Klinkers Rood-Bruin"),
                CreateGoal($"g-{year}-i3-q2", today, year, "Q2", 780m, 810m, "ahead", "i3", "Tegels 60x60 Grijs"),
                CreateGoal($"g-{year}-i4-q2", today, year, "Q2", 600m, 580m, "on-track", "i4", "Betonblokken 40x20")
            ];
        }

        if (year == today.Year)
        {
            return
            [
                CreateGoal($"g-{year}-q1", today, year, "Q1", 480_000m, 452_000m, "on-track"),
                CreateGoal($"g-{year}-q2", today, year, "Q2", 500_000m, 437_500m, "on-track"),
                CreateGoal($"g-{year}-q3", today, year, "Q3", 520_000m, 318_000m, "behind"),
                CreateGoal($"g-{year}-q4", today, year, "Q4", 550_000m, 125_000m, "behind"),
                CreateGoal($"g-{year}-i1-q2", today, year, "Q2", 1200m, 980m, "on-track", "i1", "Betonstraatstenen Antraciet"),
                CreateGoal($"g-{year}-i2-q2", today, year, "Q2", 900m, 640m, "on-track", "i2", "Klinkers Rood-Bruin"),
                CreateGoal($"g-{year}-i3-q2", today, year, "Q2", 800m, 760m, "ahead", "i3", "Tegels 60x60 Grijs"),
                CreateGoal($"g-{year}-i4-q2", today, year, "Q2", 650m, 410m, "behind", "i4", "Betonblokken 40x20")
            ];
        }

        return [];
    }

    private static GoalRecord CreateGoal(
        string id,
        DateOnly today,
        int year,
        string period,
        decimal target,
        decimal seededActual,
        string status,
        string? itemId = null,
        string? itemName = null) => new()
    {
        Id = id,
        Year = year,
        Period = period,
        Type = "Revenue",
        TargetValue = target,
        ActualValue = ResolveGoalActual(today, year, period, seededActual),
        Status = status,
        ItemId = itemId,
        ItemName = itemName
    };

    /// <summary>
    /// Zeroes out actuals for future quarters so demo goals stay realistic.
    /// </summary>
    private static decimal ResolveGoalActual(DateOnly today, int year, string period, decimal seededActual)
    {
        var periodIndex = PeriodHelper.GetQuarterIndex(period);
        if (periodIndex == 0)
        {
            return seededActual;
        }

        if (year < today.Year)
        {
            return seededActual;
        }

        if (year > today.Year)
        {
            return 0m;
        }

        var currentIndex = PeriodHelper.GetQuarterIndex(PeriodHelper.GetQuarter(today));
        return periodIndex > currentIndex ? 0m : seededActual;
    }

    private static InventoryItemRecord CreateItem(
        string itemId,
        string itemName,
        string barcode,
        decimal quantity,
        decimal reorderPoint,
        decimal salesVolume,
        bool isLowStock) => new()
    {
        ItemId = itemId,
        ItemName = itemName,
        Barcode = barcode,
        QuantityOnHand = quantity,
        ReorderPoint = reorderPoint,
        RecentSalesVolume = salesVolume,
        IsLowStock = isLowStock
    };

    private static StockLocationRecord CreateLocation(string itemId, string locationId, string locationName, decimal quantity) => new()
    {
        ItemId = itemId,
        LocationId = locationId,
        LocationName = locationName,
        QuantityOnHand = quantity
    };

    private static CustomerRecord[] GetDemoCustomers() =>
    [
        CreateCustomer("c1", "Bouwbedrijf Jansen", "Utrecht", "inkoop@jansen.nl", "Aannemer", "Woningbouw"),
        CreateCustomer("c2", "Gemeente Utrecht", "Utrecht", "aanbesteding@utrecht.nl", "Overheid", "Gemeente"),
        CreateCustomer("c3", "Stratenmakers BV", "Amersfoort", "orders@stratenmakers.nl", "Stratenmaker", "Infra"),
        CreateCustomer("c4", "Woningbouw De Vliet", "Nieuwegein", "inkoop@devliet.nl", "Aannemer", "Woningbouw"),
        CreateCustomer("c5", "Aannemer Noord", "Groningen", "sales@aannemernoord.nl", "Aannemer", "Infra")
    ];

    /// <summary>
    /// Inserts demo customers when the Customers table is empty on an already-seeded database.
    /// </summary>
    private static async Task EnsureDemoCustomersAsync(
        TebiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Customers.AddRange(GetDemoCustomers());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Links known demo sales orders to customers when CustomerId was not stored yet.
    /// </summary>
    private static SupplierRecord[] GetDemoSuppliers() =>
    [
        CreateSupplier("s1", "BetonCentrale Midden-NL", "Nieuwegein", "inkoop@betoncentrale.nl"),
        CreateSupplier("s2", "Klinker Import BV", "Rotterdam", "orders@klinkerimport.nl"),
        CreateSupplier("s3", "Logistics Partner BV", "Utrecht", "transport@logistics.nl"),
        CreateSupplier("s4", "Energiebedrijf NL", "Arnhem", "zakelijk@energie.nl"),
        CreateSupplier("s5", "Technische Dienst BV", "Amersfoort", "service@technisch.nl"),
        CreateSupplier("s6", "TegelGroothandel BV", "Utrecht", "handel@tegelgroothandel.nl"),
        CreateSupplier("s7", "Media Studio Utrecht", "Utrecht", "info@mediautrecht.nl"),
        CreateSupplier("s8", "Verzekeraar NL", "Den Haag", "zakelijk@verzekeraar.nl")
    ];

    /// <summary>
    /// Inserts demo suppliers when the Suppliers table is empty on an already-seeded database.
    /// </summary>
    private static async Task EnsureDemoSuppliersAsync(
        TebiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Suppliers.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Suppliers.AddRange(GetDemoSuppliers());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Links known demo purchase orders to suppliers when SupplierId was not stored yet.
    /// </summary>
    private static async Task BackfillPurchaseOrderSuppliersAsync(
        TebiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var purchaseOrderSupplierLinks = new Dictionary<string, string>
        {
            ["t4"] = "s1",
            ["t15"] = "s2",
            ["t16"] = "s1",
            ["t17"] = "s6",
            ["t18"] = "s6"
        };

        var purchaseOrders = await dbContext.OpenTransactions
            .Where(t => t.Type == "Purchase Order" && t.SupplierId == null)
            .ToListAsync(cancellationToken);

        var updated = false;

        foreach (var order in purchaseOrders)
        {
            if (purchaseOrderSupplierLinks.TryGetValue(order.TransactionId, out var supplierId))
            {
                order.SupplierId = supplierId;
                updated = true;
            }
        }

        if (updated)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Links expense rows to supplier master data based on vendor name.
    /// </summary>
    private static async Task BackfillExpenseSuppliersAsync(
        TebiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var vendorToSupplier = new Dictionary<string, string>
        {
            ["Logistics Partner BV"] = "s3",
            ["Energiebedrijf NL"] = "s4",
            ["Technische Dienst BV"] = "s5",
            ["Media Studio Utrecht"] = "s7",
            ["Verzekeraar NL"] = "s8"
        };

        var expenses = await dbContext.Expenses
            .Where(e => e.SupplierId == null)
            .ToListAsync(cancellationToken);

        var updated = false;

        foreach (var expense in expenses)
        {
            if (vendorToSupplier.TryGetValue(expense.VendorName, out var supplierId))
            {
                expense.SupplierId = supplierId;
                updated = true;
            }
        }

        if (updated)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Adds recent expense lines so supplier spend appears in the default period filter.
    /// </summary>
    private static async Task EnsureDemoExpenseSpreadAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var spread = BuildDemoExpenseSpread(today);
        var spreadIds = spread.Select(expense => expense.Id).ToList();
        var existingIds = await dbContext.Expenses
            .Where(expense => spreadIds.Contains(expense.Id))
            .Select(expense => expense.Id)
            .ToListAsync(cancellationToken);

        var missing = spread.Where(expense => !existingIds.Contains(expense.Id)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        dbContext.Expenses.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds extra purchase orders spread across recent days for supplier charts and lists.
    /// </summary>
    private static async Task EnsureDemoPurchaseOrderSpreadAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var spread = BuildDemoPurchaseOrderSpread(today);
        var spreadIds = spread.Select(order => order.TransactionId).ToList();
        var existingIds = await dbContext.OpenTransactions
            .Where(order => spreadIds.Contains(order.TransactionId))
            .Select(order => order.TransactionId)
            .ToListAsync(cancellationToken);

        var missing = spread.Where(order => !existingIds.Contains(order.TransactionId)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        dbContext.OpenTransactions.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Extra purchase-order lines that share a parent id ("t19-2", "2136801-1") so the supplier page can group them.
    /// </summary>
    private static async Task EnsureDemoPurchaseOrderGroupingLinesAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (await dbContext.OpenTransactions.AnyAsync(order => order.TransactionId == "t19-2", cancellationToken))
        {
            return;
        }

        dbContext.OpenTransactions.AddRange(BuildDemoPurchaseOrderGroupingLines(today));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Extra lines on t19 plus a NetSuite-style "{order}-{line}" document for BetonCentrale.
    private static OpenTransactionRecord[] BuildDemoPurchaseOrderGroupingLines(DateOnly today) =>
    [
        CreateTransaction("t19-2", "Purchase Order", "i2", today.AddDays(-2), 20, "Pending Receipt", supplierId: "s1", amount: 24000m),
        CreateTransaction("t19-3", "Purchase Order", "i6", today.AddDays(-2), 15, "Pending Receipt", supplierId: "s1", amount: 6750m),
        CreateTransaction("t19-4", "Purchase Order", "i8", today.AddDays(-2), 8, "Pending Receipt", supplierId: "s1", amount: 4000m),
        CreateTransaction("2136801-1", "Purchase Order", "i4", today.AddDays(-1), 12, "Pending Receipt", supplierId: "s1", amount: 2880m),
        CreateTransaction("2136801-2", "Purchase Order", "i2", today.AddDays(-1), 18, "Pending Receipt", supplierId: "s1", amount: 21600m),
        CreateTransaction("2136801-3", "Purchase Order", "i3", today.AddDays(-1), 10, "Pending Receipt", supplierId: "s1", amount: 5400m),
        CreateTransaction("2136801-4", "Purchase Order", "i6", today.AddDays(-1), 6, "Pending Receipt", supplierId: "s1", amount: 2700m)
    ];

    /// <summary>
    /// Keeps sales and purchase order amounts aligned with revenue and expense totals.
    /// </summary>
    private static async Task EnsureDemoOrderAmountAlignmentAsync(
        TebiDashboardDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var windowStart = today.AddDays(-120);

        var revenueRows = await dbContext.RevenueRecords
            .Where(revenue => revenue.Date >= windowStart && revenue.Date <= today)
            .OrderBy(revenue => revenue.CustomerId)
            .ThenBy(revenue => revenue.Date)
            .ToListAsync(cancellationToken);

        foreach (var customerGroup in revenueRows.GroupBy(revenue => revenue.CustomerId))
        {
            var customerId = customerGroup.Key;
            var targetTotal = customerGroup.Sum(revenue => revenue.Amount);

            var salesOrders = await dbContext.OpenTransactions
                .Where(transaction => transaction.Type == "Sales Order"
                    && transaction.CustomerId == customerId
                    && transaction.TransactionDate >= windowStart
                    && transaction.TransactionDate <= today)
                .OrderBy(transaction => transaction.TransactionDate)
                .ToListAsync(cancellationToken);

            if (salesOrders.Count == 0)
            {
                foreach (var revenue in customerGroup)
                {
                    var linkedOrderId = $"t-so-{revenue.Id}";
                    if (await dbContext.OpenTransactions.AnyAsync(
                            transaction => transaction.TransactionId == linkedOrderId,
                            cancellationToken))
                    {
                        continue;
                    }

                    var itemId = MapProductGroupToItem(revenue.ProductGroupId);
                    var unitPrice = GetDemoSalesUnitPrice(itemId);
                    var quantity = Math.Max(1m, Math.Round(revenue.Amount / unitPrice, 0));

                    dbContext.OpenTransactions.Add(CreateTransaction(
                        linkedOrderId,
                        "Sales Order",
                        itemId,
                        revenue.Date,
                        quantity,
                        "Pending Fulfillment",
                        customerId: customerId,
                        amount: revenue.Amount));
                }

                continue;
            }

            DistributeOrderAmounts(salesOrders, targetTotal);
        }

        var purchaseOrders = await dbContext.OpenTransactions
            .Where(transaction => transaction.Type == "Purchase Order")
            .ToListAsync(cancellationToken);

        foreach (var purchaseOrder in purchaseOrders)
        {
            if (purchaseOrder.Amount > 0)
            {
                continue;
            }

            purchaseOrder.Amount = Math.Round(
                purchaseOrder.QuantityOpen * GetDemoPurchaseUnitPrice(purchaseOrder.ItemId),
                0);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void DistributeOrderAmounts(IList<OpenTransactionRecord> orders, decimal targetTotal)
    {
        if (orders.Count == 0)
        {
            return;
        }

        if (targetTotal <= 0)
        {
            foreach (var order in orders)
            {
                order.Amount = 0m;
            }

            return;
        }

        var share = Math.Round(targetTotal / orders.Count, 0, MidpointRounding.AwayFromZero);
        var assigned = 0m;

        for (var index = 0; index < orders.Count; index++)
        {
            var amount = index == orders.Count - 1
                ? targetTotal - assigned
                : share;
            orders[index].Amount = amount;
            assigned += amount;
        }
    }

    private static string MapProductGroupToItem(string productGroupId) => productGroupId switch
    {
        "pg-tiles" => "i3",
        "pg-edging" => "i5",
        _ => "i1"
    };

    private static decimal GetDemoSalesUnitPrice(string itemId) => itemId switch
    {
        "i2" => 1200m,
        "i3" => 950m,
        "i4" => 420m,
        "i5" => 780m,
        "i6" => 65m,
        "i7" => 880m,
        "i8" => 1100m,
        "i10" => 720m,
        _ => 850m
    };

    private static decimal GetDemoPurchaseUnitPrice(string itemId) => itemId switch
    {
        "i2" => 680m,
        "i3" => 540m,
        "i4" => 240m,
        "i6" => 38m,
        "i8" => 620m,
        _ => 480m
    };

    private static async Task BackfillSalesOrderCustomersAsync(
        TebiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var salesOrderCustomerLinks = new Dictionary<string, string>
        {
            ["t1"] = "c1",
            ["t3"] = "c3",
            ["t5"] = "c2"
        };

        var salesOrders = await dbContext.OpenTransactions
            .Where(t => t.Type == "Sales Order" && t.CustomerId == null)
            .ToListAsync(cancellationToken);

        var updated = false;

        foreach (var order in salesOrders)
        {
            if (salesOrderCustomerLinks.TryGetValue(order.TransactionId, out var customerId))
            {
                order.CustomerId = customerId;
                updated = true;
            }
        }

        if (updated)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static CustomerRecord CreateCustomer(
        string customerId,
        string name,
        string city,
        string contactEmail,
        string category = "",
        string subcategory = "") => new()
    {
        CustomerId = customerId,
        Name = name,
        City = city,
        ContactEmail = contactEmail,
        Category = category,
        Subcategory = subcategory
    };

    private static SupplierRecord CreateSupplier(
        string supplierId,
        string name,
        string city,
        string contactEmail) => new()
    {
        SupplierId = supplierId,
        Name = name,
        City = city,
        ContactEmail = contactEmail
    };

    private static OpenTransactionRecord CreateTransaction(
        string transactionId,
        string type,
        string itemId,
        DateOnly transactionDate,
        decimal quantityOpen,
        string status,
        string? customerId = null,
        string? supplierId = null,
        decimal amount = 0m) => new()
    {
        TransactionId = transactionId,
        Type = type,
        ItemId = itemId,
        ItemName = DemoItemName(itemId),
        CustomerId = customerId,
        SupplierId = supplierId,
        TransactionDate = transactionDate,
        QuantityOpen = quantityOpen,
        Amount = amount,
        Status = status
    };

    private static ExpenseRecord CreateExpense(
        string id,
        DateOnly date,
        string category,
        decimal amount,
        string vendor,
        string? supplierId = null) => new()
    {
        Id = id,
        Date = date,
        Category = category,
        Amount = amount,
        VendorName = vendor,
        SupplierId = supplierId
    };
}
