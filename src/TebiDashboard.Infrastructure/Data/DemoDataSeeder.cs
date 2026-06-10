using Microsoft.EntityFrameworkCore;
using TebiDashboard.Infrastructure.Data.Entities;
using TebiDashboard.Shared.Constants;

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

        // Backfill customer master data when the table was added after an existing deployment.
        await EnsureDemoCustomersAsync(dbContext, cancellationToken);
        await BackfillSalesOrderCustomersAsync(dbContext, cancellationToken);

        if (await dbContext.RevenueRecords.AnyAsync(cancellationToken))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var year = today.Year;

        dbContext.RevenueRecords.AddRange(
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
            CreateRevenue("r15", today.AddDays(-3), 39500m, "c1", "Bouwbedrijf Jansen", "pg-tiles"));

        dbContext.Goals.AddRange(
            CreateGoal("g-q1", year, "Q1", 480_000m, 452_000m, "on-track"),
            CreateGoal("g-q2", year, "Q2", 500_000m, 437_500m, "on-track"),
            CreateGoal("g-q3", year, "Q3", 520_000m, 318_000m, "behind"),
            CreateGoal("g-q4", year, "Q4", 550_000m, 125_000m, "behind"),
            CreateGoal("g-i1", year, "Q2", 1200m, 980m, "on-track", "i1", "Betonstraatstenen Antraciet"),
            CreateGoal("g-i2", year, "Q2", 900m, 640m, "on-track", "i2", "Klinkers Rood-Bruin"),
            CreateGoal("g-i3", year, "Q2", 800m, 760m, "ahead", "i3", "Tegels 60x60 Grijs"),
            CreateGoal("g-i4", year, "Q2", 650m, 410m, "behind", "i4", "Betonblokken 40x20"));

        dbContext.InventoryItems.AddRange(
            CreateItem("i1", "Betonstraatstenen Antraciet", "8712345000011", 450, 100, 980, false),
            CreateItem("i2", "Klinkers Rood-Bruin", "8712345000028", 22, 80, 640, true),
            CreateItem("i3", "Tegels 60x60 Grijs", "8712345000035", 210, 120, 760, false),
            CreateItem("i4", "Betonblokken 40x20", "8712345000042", 38, 90, 410, true),
            CreateItem("i5", "Opsluitbanden Grijs", "8712345000059", 520, 150, 320, false),
            CreateItem("i6", "Grind 16-32 mm", "8712345000066", 18, 60, 280, true),
            CreateItem("i7", "Zandstraatstenen Geel", "8712345000073", 175, 100, 540, false),
            CreateItem("i8", "Keramische Tegels Antraciet", "8712345000080", 95, 70, 490, false),
            CreateItem("i9", "Drainage Tegels", "8712345000097", 12, 50, 210, true),
            CreateItem("i10", "Beton Tegels 50x50", "8712345000103", 340, 110, 670, false));

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

        dbContext.OpenTransactions.AddRange(
            CreateTransaction("t1", "Sales Order", "i1", today.AddDays(-1), 40, "Pending Fulfillment", "c1"),
            CreateTransaction("t2", "Transfer Order", "i1", today.AddDays(-2), 20, "Pending Receipt"),
            CreateTransaction("t3", "Sales Order", "i2", today.AddDays(-5), 55, "Pending Fulfillment", "c3"),
            CreateTransaction("t4", "Purchase Order", "i4", today.AddDays(-12), 120, "Pending Receipt"),
            CreateTransaction("t5", "Sales Order", "i3", today.AddDays(-3), 30, "Pending Fulfillment", "c2"),
            CreateTransaction("t6", "Transfer Order", "i6", today.AddDays(-18), 45, "In Transit"));

        dbContext.Expenses.AddRange(
            CreateExpense("e1", today.AddDays(-80), "Transport", 12500m, "Logistics Partner BV"),
            CreateExpense("e2", today.AddDays(-65), "Utilities", 4200m, "Energiebedrijf NL"),
            CreateExpense("e3", today.AddDays(-50), "Maintenance", 6800m, "Technische Dienst BV"),
            CreateExpense("e4", today.AddDays(-40), "Transport", 9800m, "Logistics Partner BV"),
            CreateExpense("e5", today.AddDays(-28), "Marketing", 3500m, "Media Studio Utrecht"),
            CreateExpense("e6", today.AddDays(-18), "Utilities", 4100m, "Energiebedrijf NL"),
            CreateExpense("e7", today.AddDays(-12), "Insurance", 7200m, "Verzekeraar NL"),
            CreateExpense("e8", today.AddDays(-5), "Maintenance", 2950m, "Technische Dienst BV"));

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
        dbContext.InventoryItems.RemoveRange(dbContext.InventoryItems);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RevenueRecord CreateRevenue(
        string id,
        DateOnly date,
        decimal amount,
        string customerId,
        string customerName,
        string productGroupId) => new()
    {
        Id = id,
        Date = date,
        Amount = amount,
        CustomerId = customerId,
        CustomerName = customerName,
        ProductGroupId = productGroupId
    };

    private static GoalRecord CreateGoal(
        string id,
        int year,
        string period,
        decimal target,
        decimal actual,
        string status,
        string? itemId = null,
        string? itemName = null) => new()
    {
        Id = id,
        Year = year,
        Period = period,
        Type = "Revenue",
        TargetValue = target,
        ActualValue = actual,
        Status = status,
        ItemId = itemId,
        ItemName = itemName
    };

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
        CreateCustomer("c1", "Bouwbedrijf Jansen", "Utrecht", "inkoop@jansen.nl"),
        CreateCustomer("c2", "Gemeente Utrecht", "Utrecht", "aanbesteding@utrecht.nl"),
        CreateCustomer("c3", "Stratenmakers BV", "Amersfoort", "orders@stratenmakers.nl"),
        CreateCustomer("c4", "Woningbouw De Vliet", "Nieuwegein", "inkoop@devliet.nl"),
        CreateCustomer("c5", "Aannemer Noord", "Groningen", "sales@aannemernoord.nl")
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
        string contactEmail) => new()
    {
        CustomerId = customerId,
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
        string? customerId = null) => new()
    {
        TransactionId = transactionId,
        Type = type,
        ItemId = itemId,
        CustomerId = customerId,
        TransactionDate = transactionDate,
        QuantityOpen = quantityOpen,
        Status = status
    };

    private static ExpenseRecord CreateExpense(string id, DateOnly date, string category, decimal amount, string vendor) => new()
    {
        Id = id,
        Date = date,
        Category = category,
        Amount = amount,
        VendorName = vendor
    };
}
