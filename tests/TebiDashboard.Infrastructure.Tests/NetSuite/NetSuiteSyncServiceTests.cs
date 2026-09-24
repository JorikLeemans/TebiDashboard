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
                Row(("id", "\"c-old\""), ("glommedName", "\"Renamed customer\""), ("email", "\"old@tebi.nl\""), ("city", "\"Eindhoven\""), ("category", "\"Aannemer\""), ("subcategory", "\"Woningbouw\"")),
                Row(("id", "\"c-new\""), ("glommedName", "\"New customer\""), ("email", "\"new@tebi.nl\""), ("city", "\"Utrecht\""), ("category", "\"Overheid\""), ("subcategory", "\"16\""))
            ],
            Invoices =
            [
                // Same key as the stored line, with a different amount that must not overwrite it.
                InvoiceRow("inv-old", "1", "2024-01-15", "c-old", 99.00m),
                InvoiceRow("inv-new", "1", "2026-09-01", "c-new", 25.50m)
            ]
        };

        var result = await CreateSut(db, restClient, clearOnSync: false)
            .SyncDashboardDataAsync();

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

    [Fact]
    public async Task Sync_UsesGlommedName_WhenCompanyNameIsAlsoPresent()
    {
        await using var db = CreateDbContext();
        var restClient = new FakeNetSuiteRestClient
        {
            Customers =
            [
                Row(
                    ("id", "\"c-1\""),
                    ("companyName", "\"Company only\""),
                    ("glommedName", "\"Jansen, Piet\""),
                    ("email", "\"piet@tebi.nl\""),
                    ("city", "\"Helmond\""))
            ]
        };

        var result = await CreateSut(db, restClient, clearOnSync: false)
            .SyncDashboardDataAsync();

        Assert.True(result.Success, result.Message);
        var customer = await db.Customers.SingleAsync();
        Assert.Equal("Jansen, Piet", customer.Name);
        Assert.Equal("Helmond", customer.City);
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
            .SyncDashboardDataAsync();

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, await db.StockLocations.CountAsync());
        Assert.Equal(8m, (await db.StockLocations.SingleAsync()).QuantityOnHand);
        Assert.Equal(8m, (await db.InventoryItems.SingleAsync()).QuantityOnHand);
        Assert.Equal("SKU-1", (await db.InventoryItems.SingleAsync()).ItemName);
    }

    private static NetSuiteSyncService CreateSut(
        TebiDashboardDbContext db,
        INetSuiteRestClient restClient,
        bool clearOnSync)
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

        return new NetSuiteSyncService(db, restClient, options, NullLogger<NetSuiteSyncService>.Instance);
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
}
