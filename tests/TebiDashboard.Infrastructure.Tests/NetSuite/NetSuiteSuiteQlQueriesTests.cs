using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

public sealed class NetSuiteSuiteQlQueriesTests
{
    [Fact]
    public void Customers_IncludesTebiFieldNames()
    {
        var sql = NetSuiteSuiteQlQueries.Customers();

        Assert.Contains("entityid", sql);
        Assert.Contains("companyname", sql);
        Assert.DoesNotContain("defaultaddress", sql);
    }

    [Fact]
    public void InventoryItems_UsesItemIdInsteadOfDisplayName()
    {
        var sql = NetSuiteSuiteQlQueries.InventoryItems();

        Assert.Contains("itemid", sql);
        Assert.DoesNotContain("displayname", sql);
    }

    [Fact]
    public void Invoices_SumsNonTaxLineAmountsExclVat()
    {
        var sql = NetSuiteSuiteQlQueries.Invoices(new DateOnly(2026, 1, 1));

        Assert.Contains("tl.taxline = 'F'", sql);
        Assert.Contains("subsidiary", sql);
        Assert.DoesNotContain("subtotal", sql);
        Assert.DoesNotContain("LEFT JOIN customer", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VendorBills_UsesForeignTotalWithoutJoin()
    {
        var sql = NetSuiteSuiteQlQueries.VendorBills(new DateOnly(2026, 1, 1));

        Assert.Contains("foreigntotal", sql);
        Assert.DoesNotContain("LEFT JOIN vendor", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StockLocations_AliasesQuantityOnHandAsQuantity()
    {
        var sql = NetSuiteSuiteQlQueries.StockLocations();

        Assert.Contains("quantityonhand AS quantity", sql);
        Assert.Contains("inventorybalance", sql);
    }
}
