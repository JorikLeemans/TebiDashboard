using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.Data.Entities;
using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Incremental sync must keep stored dashboard rows and only add unseen NetSuite records.
/// </summary>
public sealed class NetSuiteSyncServiceTests
{
    [Fact]
    public async Task Sync_AppendsNewRevenue_AndLeavesExistingRowsUntouched()
    {
        await using var db = CreateDbContext();
        db.Customers.Add(new CustomerRecord
        {
            CustomerId = "c-old",
            Name = "Existing customer",
            City = "Eindhoven",
            ContactEmail = "old@tebi.nl"
        });
        db.RevenueRecords.Add(new RevenueRecord
        {
            Id = "inv-old-1",
            InvoiceId = "inv-old",
            InvoiceNumber = "F-100",
            Date = new DateOnly(2024, 1, 15),
            Amount = 10.00m,
            CustomerId = "c-old",
            CustomerName = "Existing customer"
        });
        await db.SaveChangesAsync();

        var restClient = new FakeNetSuiteRestClient
        {
            Customers =
            [
                Row(("id", "\"c-old\""), ("entityId", "\"Renamed customer\""), ("glommedName", "\"Other label\""), ("email", "\"old@tebi.nl\""), ("city", "\"Eindhoven\""), ("category", "\"Aannemer\""), ("subcategory", "\"Woningbouw\"")),
                Row(("id", "\"c-new\""), ("entityId", "\"New customer\""), ("glommedName", "\"Other new label\""), ("email", "\"new@tebi.nl\""), ("city", "\"Utrecht\""), ("category", "\"Overheid\""), ("subcategory", "\"16\""))
            ],
            Invoices =
            [
                // Same key as the stored line, with a different amount that must not overwrite it.
                InvoiceRow("inv-old", "1", "2024-01-15", "c-old", 99.00m),
                InvoiceRow("inv-new", "1", "2026-09-01", "c-new", 25.50m)
            ]
        };

        var result = await CreateSut(db, restClient, clearOnSync: false)
            .SyncDashboardDataAsync(365);

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, result.RevenueRecordsSynced);
        Assert.Equal(2, await db.RevenueRecords.CountAsync());

        var existing = await db.RevenueRecords.SingleAsync(row => row.Id == "inv-old-1");
        Assert.Equal(10.00m, existing.Amount);
        Assert.Equal("F-100", existing.InvoiceNumber);

        var added = await db.RevenueRecords.SingleAsync(row => row.Id == "inv-new-1");
        Assert.Equal(25.50m, added.Amount);
        Assert.Equal("c-new", added.CustomerId);

        Assert.Equal("Renamed customer", (await db.Customers.SingleAsync(row => row.CustomerId == "c-old")).Name);
        Assert.Equal("Eindhoven", (await db.Customers.SingleAsync(row => row.CustomerId == "c-old")).City);
        Assert.Equal("Aannemer", (await db.Customers.SingleAsync(row => row.CustomerId == "c-old")).Category);
        Assert.Equal("Woningbouw", (await db.Customers.SingleAsync(row => row.CustomerId == "c-old")).Subcategory);
        Assert.Equal("Utrecht", (await db.Customers.SingleAsync(row => row.CustomerId == "c-new")).City);
        Assert.Equal("Overheid", (await db.Customers.SingleAsync(row => row.CustomerId == "c-new")).Category);
        Assert.Equal(string.Empty, (await db.Customers.SingleAsync(row => row.CustomerId == "c-new")).Subcategory);
    }

    // Invoice item lines are stored as credits in NetSuite; flip leftover negatives on sync.
    [Fact]
    public async Task Sync_FlipsStoredNegativeInvoiceAmounts_ToPositiveOmzet()
    {
        await using var db = CreateDbContext();
        db.RevenueRecords.Add(new RevenueRecord
        {
            Id = "2136821-10",
            InvoiceId = "2136821",
            InvoiceNumber = "2136821",
            Date = new DateOnly(2026, 2, 12),
            Amount = -324.00m,
            CustomerId = "c-old",
            CustomerName = "Existing customer"
        });
        await db.SaveChangesAsync();

        var result = await CreateSut(db, new FakeNetSuiteRestClient(), clearOnSync: false)
            .SyncDashboardDataAsync(365);

        Assert.True(result.Success, result.Message);
        var stored = await db.RevenueRecords.SingleAsync(row => row.Id == "2136821-10");
        Assert.Equal(324.00m, stored.Amount);
    }

    [Fact]
    public async Task Sync_CombinesEntityId_WithCompanyNameOrGlommedName()
    {
        await using var db = CreateDbContext();
        var restClient = new FakeNetSuiteRestClient
        {
            Customers =
            [
                Row(
                    ("id", "\"c-company\""),
                    ("entityId", "\"K12345\""),
                    ("isPerson", "\"F\""),
                    ("companyName", "\"Bouwbedrijf Jansen\""),
                    ("glommedName", "\"Should not use person name\""),
                    ("email", "\"inkoop@jansen.nl\""),
                    ("city", "\"Eindhoven\"")),
                Row(
                    ("id", "\"c-person\""),
                    ("entityId", "\"K67890\""),
                    ("isPerson", "\"T\""),
                    ("companyName", "\"Should not use company name\""),
                    ("glommedName", "\"Piet Jansen\""),
                    ("email", "\"piet@tebi.nl\""),
                    ("city", "\"Helmond\""))
            ]
        };

        var result = await CreateSut(db, restClient, clearOnSync: false)
            .SyncDashboardDataAsync(365);

        Assert.True(result.Success, result.Message);
        Assert.Equal(
            "K12345 Bouwbedrijf Jansen",
            (await db.Customers.SingleAsync(row => row.CustomerId == "c-company")).Name);
        Assert.Equal(
            "K67890 Piet Jansen",
            (await db.Customers.SingleAsync(row => row.CustomerId == "c-person")).Name);
        Assert.Equal("Helmond", (await db.Customers.SingleAsync(row => row.CustomerId == "c-person")).City);
    }

    [Fact]
    public async Task Sync_DoesNotDuplicateStockLocations_WhenTheSameBalanceIsImportedAgain()
    {
        await using var db = CreateDbContext();
        db.InventoryItems.Add(new InventoryItemRecord
        {
            ItemId = "item-1",
            ItemName = "Old name",
            QuantityOnHand = 3m
        });
        db.StockLocations.Add(new StockLocationRecord
        {
            ItemId = "item-1",
            LocationId = "yard",
            LocationName = "yard",
            QuantityOnHand = 3m
        });
        await db.SaveChangesAsync();

        var restClient = new FakeNetSuiteRestClient
        {
            InventoryItems =
            [
                Row(("id", "\"item-1\""), ("itemId", "\"SKU-1\""), ("upcCode", "\"123\""))
            ],
            StockLocations =
            [
                Row(("item", "\"item-1\""), ("location", "\"yard\""), ("quantity", "8"))
            ]
        };

        var result = await CreateSut(db, restClient, clearOnSync: false)
            .SyncDashboardDataAsync(365);

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, await db.StockLocations.CountAsync());
        Assert.Equal(8m, (await db.StockLocations.SingleAsync()).QuantityOnHand);
        Assert.Equal(8m, (await db.InventoryItems.SingleAsync()).QuantityOnHand);
        Assert.Equal("SKU-1", (await db.InventoryItems.SingleAsync()).ItemName);
    }

    [Fact]
    public async Task Sync_StoresLocationDisplayName_AndDoesNotKeepNumericIds()
    {
        await using var db = CreateDbContext();
        db.InventoryItems.Add(new InventoryItemRecord
        {
            ItemId = "item-1",
            ItemName = "SKU-1",
            QuantityOnHand = 3m
        });
        db.StockLocations.Add(new StockLocationRecord
        {
            ItemId = "item-1",
            LocationId = "16",
            LocationName = "16",
            QuantityOnHand = 3m
        });
        await db.SaveChangesAsync();

        var restClient = new FakeNetSuiteRestClient
        {
            InventoryItems =
            [
                Row(("id", "\"item-1\""), ("itemId", "\"SKU-1\""), ("upcCode", "\"123\""))
            ],
            StockLocations =
            [
                Row(("item", "\"item-1\""), ("location", "\"16\""), ("locationname", "\"Hoofdmagazijn\""), ("quantity", "8"))
            ]
        };

        var result = await CreateSut(db, restClient, clearOnSync: false)
            .SyncDashboardDataAsync(365);

        Assert.True(result.Success, result.Message);
        var location = await db.StockLocations.SingleAsync();
        Assert.Equal("Hoofdmagazijn", location.LocationName);
        Assert.Equal("16", location.LocationId);
    }

    [Fact]
    public async Task Sync_ClearsNumericLocationNames_WhenDisplayNameIsMissing()
    {
        await using var db = CreateDbContext();
        db.InventoryItems.Add(new InventoryItemRecord
        {
            ItemId = "item-1",
            ItemName = "SKU-1",
            QuantityOnHand = 3m
        });
        db.StockLocations.Add(new StockLocationRecord
        {
            ItemId = "item-1",
            LocationId = "16",
            LocationName = "16",
            QuantityOnHand = 3m
        });
        await db.SaveChangesAsync();

        var restClient = new FakeNetSuiteRestClient
        {
            InventoryItems =
            [
                Row(("id", "\"item-1\""), ("itemId", "\"SKU-1\""), ("upcCode", "\"123\""))
            ],
            StockLocations =
            [
                Row(("item", "\"item-1\""), ("location", "\"16\""), ("quantity", "8"))
            ]
        };

        var result = await CreateSut(db, restClient, clearOnSync: false)
            .SyncDashboardDataAsync(365);

        Assert.True(result.Success, result.Message);
        Assert.Equal(string.Empty, (await db.StockLocations.SingleAsync()).LocationName);
    }

    [Fact]
    public async Task Sync_UsesLocationReorderPoint_ForLowStockEvenWhenTotalIsAboveThreshold()
    {
        await using var db = CreateDbContext();
        var restClient = new FakeNetSuiteRestClient
        {
            InventoryItems =
            [
                Row(("id", "\"item-1\""), ("itemId", "\"SKU-1\""), ("upcCode", "\"123\""))
            ],
            StockLocations =
            [
                Row(("item", "\"item-1\""), ("location", "\"1\""), ("locationname", "\"Hoofdmagazijn\""), ("quantity", "100")),
                Row(("item", "\"item-1\""), ("location", "\"2\""), ("locationname", "\"Terrein A\""), ("quantity", "5"))
            ],
            ItemLocationConfigurations =
            [
                Row(("item", "\"item-1\""), ("location", "\"1\""), ("locationname", "\"Hoofdmagazijn\""), ("reorderpoint", "20"), ("preferredstocklevel", "50")),
                Row(("item", "\"item-1\""), ("location", "\"2\""), ("locationname", "\"Terrein A\""), ("reorderpoint", "40"), ("preferredstocklevel", "80"))
            ]
        };

        var result = await CreateSut(db, restClient, clearOnSync: false)
            .SyncDashboardDataAsync(365);

        Assert.True(result.Success, result.Message);
        var item = await db.InventoryItems.SingleAsync();
        Assert.Equal(105m, item.QuantityOnHand);
        Assert.Equal(60m, item.ReorderPoint);
        Assert.True(item.IsLowStock);

        var terrein = await db.StockLocations.SingleAsync(location => location.LocationId == "2");
        Assert.Equal(40m, terrein.ReorderPoint);
        Assert.Equal(80m, terrein.PreferredStockLevel);
        Assert.Equal(5m, terrein.QuantityOnHand);
    }

    [Fact]
    public async Task Sync_ReportsStepProgress_ToCoordinator()
    {
        await using var db = CreateDbContext();
        var coordinator = new RecordingSyncCoordinator();

        var result = await CreateSut(db, new FakeNetSuiteRestClient(), clearOnSync: false, coordinator)
            .SyncDashboardDataAsync(365);

        Assert.True(result.Success, result.Message);
        Assert.Contains(coordinator.Reports, report => report.CurrentStep == "starting" && report.TotalSteps == 8);
        Assert.Contains(coordinator.Reports, report => report.CurrentStep == "customers");
        Assert.Contains(coordinator.Reports, report => report.CurrentStep == "invoices");
        Assert.Contains(coordinator.Reports, report => report.CurrentStep == "recent item sales");
        Assert.Equal(8, coordinator.Reports.Max(report => report.CompletedSteps));
        Assert.All(coordinator.Reports, report => Assert.Equal(8, report.TotalSteps));
    }

    private static NetSuiteSyncService CreateSut(
        TebiDashboardDbContext db,
        INetSuiteRestClient restClient,
        bool clearOnSync,
        INetSuiteSyncCoordinator? coordinator = null)
    {
        var options = Options.Create(new NetSuiteOptions
        {
            Enabled = true,
            AccountId = "1234567",
            ClientId = "client",
            CertificateId = "cert",
            PrivateKeyPem = "-----BEGIN PRIVATE KEY-----\ntest\n-----END PRIVATE KEY-----",
            ClearDashboardDataOnSync = clearOnSync,
            SyncLookbackDays = 365,
            SyncPageSize = 200
        });

        return new NetSuiteSyncService(
            db,
            restClient,
            options,
            NullLogger<NetSuiteSyncService>.Instance,
            coordinator ?? new NoOpSyncCoordinator());
    }

    private static TebiDashboardDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TebiDashboardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TebiDashboardDbContext(options);
    }

    private static Dictionary<string, JsonElement> InvoiceRow(
        string invoiceId,
        string lineId,
        string tranDate,
        string customerId,
        decimal amount) =>
        Row(
            ("invoiceid", $"\"{invoiceId}\""),
            ("invoicenumber", $"\"F-{invoiceId}\""),
            ("lineid", $"\"{lineId}\""),
            ("trandate", $"\"{tranDate}\""),
            ("customerid", $"\"{customerId}\""),
            ("itemid", "\"item-1\""),
            ("itemname", "\"SKU-1\""),
            ("productgroupid", "\"pg-1\""),
            ("productgroupname", "\"Stenen\""),
            ("amount", amount.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private static Dictionary<string, JsonElement> Row(params (string Key, string Json)[] fields)
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, json) in fields)
        {
            using var document = JsonDocument.Parse(json);
            row[key] = document.RootElement.Clone();
        }

        return row;
    }

    /// <summary>
    /// Returns canned SuiteQL pages keyed by the query text the sync service sends.
    /// </summary>
    private sealed class FakeNetSuiteRestClient : INetSuiteRestClient
    {
        public List<IReadOnlyDictionary<string, JsonElement>> Customers { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> Vendors { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> InventoryItems { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> StockLocations { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> ItemLocationConfigurations { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> Invoices { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> VendorBills { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> SalesOrders { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> PurchaseOrders { get; init; } = [];

        public List<IReadOnlyDictionary<string, JsonElement>> RecentItemSales { get; init; } = [];

        public Task<IReadOnlyList<IReadOnlyDictionary<string, JsonElement>>> QuerySuiteQlAsync(
            string sql,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> rows = sql switch
            {
                _ when sql.Contains("itemlocationconfiguration", StringComparison.OrdinalIgnoreCase) => ItemLocationConfigurations,
                _ when sql.Contains("recentsalesvolume", StringComparison.OrdinalIgnoreCase) => RecentItemSales,
                _ when sql.Contains("FROM customer", StringComparison.OrdinalIgnoreCase) => Customers,
                _ when sql.Contains("FROM vendor", StringComparison.OrdinalIgnoreCase) => Vendors,
                _ when sql.Contains("FROM item", StringComparison.OrdinalIgnoreCase) => InventoryItems,
                _ when sql.Contains("inventorybalance", StringComparison.OrdinalIgnoreCase) => StockLocations,
                _ when sql.Contains("VendBill", StringComparison.Ordinal) => VendorBills,
                _ when sql.Contains("salesorder", StringComparison.OrdinalIgnoreCase) => SalesOrders,
                _ when sql.Contains("purchaseorder", StringComparison.OrdinalIgnoreCase) => PurchaseOrders,
                _ => Invoices
            };

            return Task.FromResult(rows);
        }
    }

    /// <summary>
    /// Ignores progress reports so existing incremental-sync tests stay focused on database rows.
    /// </summary>
    private sealed class NoOpSyncCoordinator : INetSuiteSyncCoordinator
    {
        public bool TryStart(int lookbackDays) => true;

        public NetSuiteSyncRunStatus GetStatus() => new();

        public void ReportProgress(string currentStep, int completedSteps, int totalSteps)
        {
        }
    }

    /// <summary>
    /// Captures every progress callback so tests can assert step names and counts.
    /// </summary>
    private sealed class RecordingSyncCoordinator : INetSuiteSyncCoordinator
    {
        public List<(string CurrentStep, int CompletedSteps, int TotalSteps)> Reports { get; } = [];

        public bool TryStart(int lookbackDays) => true;

        public NetSuiteSyncRunStatus GetStatus() => new();

        public void ReportProgress(string currentStep, int completedSteps, int totalSteps) =>
            Reports.Add((currentStep, completedSteps, totalSteps));
    }
}
