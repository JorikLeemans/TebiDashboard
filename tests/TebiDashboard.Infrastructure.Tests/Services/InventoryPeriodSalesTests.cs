using Microsoft.EntityFrameworkCore;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.Data.Entities;
using TebiDashboard.Infrastructure.Services;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Infrastructure.Tests.Services;

/// <summary>
/// The inventory list sold column is the period invoice total, not the stored sync quantity.
/// </summary>
public sealed class InventoryPeriodSalesTests
{
    [Fact]
    public async Task InventoryList_ShowsZero_WhenTheItemHasNoInvoicesInThePeriod()
    {
        await using var db = CreateDb();
        db.InventoryItems.Add(new InventoryItemRecord
        {
            ItemId = "24851",
            ItemName = "350003 Straatzand los gestort",
            Barcode = "358883",
            QuantityOnHand = 138855m,
            ReorderPoint = 15m,
            // Stored lookback quantity. NetSuite keeps outbound quantity negative.
            RecentSalesVolume = -2_910_737m
        });
        await db.SaveChangesAsync();

        var items = await new DatabaseNetSuiteService(db).GetInventoryItemsAsync(new DateRangeFilter
        {
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 10, 1)
        });

        var sand = Assert.Single(items);
        Assert.Equal(0m, sand.RecentSalesVolume);
        Assert.Equal(0m, sand.UnitsSold);
    }

    [Fact]
    public async Task InventoryList_SumsInvoiceLines_ForTheSelectedPeriod()
    {
        await using var db = CreateDb();
        db.InventoryItems.Add(new InventoryItemRecord
        {
            ItemId = "i1",
            ItemName = "Betonstraatstenen Antraciet",
            RecentSalesVolume = -50m
        });
        db.RevenueRecords.Add(new RevenueRecord
        {
            Id = "inv-1",
            ItemId = "i1",
            Date = new DateOnly(2026, 9, 14),
            Amount = 4_500.5m,
            Quantity = 20m,
            InvoiceId = "inv",
            InvoiceNumber = "F-2026-014"
        });
        await db.SaveChangesAsync();

        var items = await new DatabaseNetSuiteService(db).GetInventoryItemsAsync(new DateRangeFilter
        {
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 9, 30)
        });

        var item = Assert.Single(items);
        Assert.Equal(4_500.5m, item.RecentSalesVolume);
        Assert.Equal(20m, item.UnitsSold);
    }

    [Fact]
    public async Task PeriodExportInventory_UsesInvoiceTotals_NotTheStoredSyncQuantity()
    {
        await using var db = CreateDb();
        db.InventoryItems.Add(new InventoryItemRecord
        {
            ItemId = "100028",
            ItemName = "100028 Schellevis Oud Hollandse dikformaat 21x7x8cm grijs",
            Barcode = "1128080009416",
            QuantityOnHand = 1436m,
            // Stored lookback quantity. The export used to print this as the sales amount.
            RecentSalesVolume = -16_270.30m
        });
        db.OpenTransactions.Add(new OpenTransactionRecord
        {
            TransactionId = "so-1",
            Type = "Sales Order",
            ItemId = "100028",
            ItemName = "100028 Schellevis Oud Hollandse dikformaat 21x7x8cm grijs",
            TransactionDate = new DateOnly(2026, 3, 1),
            QuantityOpen = 10m,
            Status = "Pending Fulfillment"
        });
        await db.SaveChangesAsync();

        var overview = await new DatabaseNetSuiteService(db).GetPeriodOverviewAsync(new DateRangeFilter
        {
            From = new DateOnly(2026, 1, 1),
            To = new DateOnly(2026, 10, 5)
        });

        var item = Assert.Single(overview.InventoryWithActivity);
        Assert.Equal(0m, item.RecentSalesVolume);
        Assert.Equal(0m, item.UnitsSold);
        Assert.Equal(1436m, item.QuantityOnHand);
    }

    private static TebiDashboardDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TebiDashboardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TebiDashboardDbContext(options);
    }
}
