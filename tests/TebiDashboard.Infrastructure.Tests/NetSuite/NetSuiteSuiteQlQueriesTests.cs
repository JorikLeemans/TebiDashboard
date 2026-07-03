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
    public void Invoices_UsesSubtotalExclVatAndSubsidiary()
    {
        var sql = NetSuiteSuiteQlQueries.Invoices(new DateOnly(2026, 1, 1));

        Assert.Contains("subtotal", sql);
        Assert.Contains("subsidiary", sql);
        Assert.DoesNotContain("foreigntotal", sql);
    }

    [Fact]
    public void VendorBills_UsesTotalAmount()
    {
        var sql = NetSuiteSuiteQlQueries.VendorBills(new DateOnly(2026, 1, 1));

        Assert.Contains("total", sql);
        Assert.DoesNotContain("foreigntotal", sql);
    }

    [Fact]
    public void StockLocations_AliasesQuantityOnHandAsQuantity()
    {
        var sql = NetSuiteSuiteQlQueries.StockLocations();

        Assert.Contains("quantityonhand AS quantity", sql);
        Assert.Contains("inventorybalance", sql);
    }
}
