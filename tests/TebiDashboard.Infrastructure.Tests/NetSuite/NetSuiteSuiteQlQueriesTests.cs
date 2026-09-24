using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

public sealed class NetSuiteSuiteQlQueriesTests
{
    [Fact]
    public void Customers_UsesTebiSuiteQlFieldNames()
    {
        var sql = NetSuiteSuiteQlQueries.Customers();

        Assert.Contains("entityId", sql);
        Assert.Contains("companyName", sql);
        Assert.Contains("defaultBillingAddress", sql);
        Assert.Contains("isInactive", sql);
        Assert.DoesNotContain("defaultaddress", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vendors_UsesTebiSuiteQlFieldNames()
    {
        var sql = NetSuiteSuiteQlQueries.Vendors();

        Assert.Contains("entityId", sql);
        Assert.Contains("companyName", sql);
        Assert.Contains("defaultBillingAddress", sql);
        Assert.Contains("isInactive", sql);
    }

    [Fact]
    public void InventoryItems_UsesTebiSuiteQlFieldNames()
    {
        var sql = NetSuiteSuiteQlQueries.InventoryItems();

        Assert.Contains("itemId", sql);
        Assert.Contains("upcCode", sql);
        Assert.DoesNotContain("reorderPoint", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("displayname", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Invoices_UsesInvoiceTableWithExclVatAmount()
    {
        var sql = NetSuiteSuiteQlQueries.Invoices(new DateOnly(2026, 1, 1));

        Assert.Contains("FROM invoice", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tranDate", sql);
        Assert.Contains("taxTotal", sql);
        Assert.Contains("subsidiary", sql);
        Assert.DoesNotContain("transactionline", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Invoices_SelectsReadableProductGroupName()
    {
        var sql = NetSuiteSuiteQlQueries.Invoices(new DateOnly(2026, 1, 1));

        // BUILTIN.DF resolves the subsidiary reference to its display name so the dashboard can
        // show a readable product group instead of the raw numeric id.
        Assert.Contains("BUILTIN.DF(subsidiary) AS productgroupname", sql);
    }

    [Fact]
    public void VendorBills_UsesForeignTotalWithoutJoin()
    {
        var sql = NetSuiteSuiteQlQueries.VendorBills(new DateOnly(2026, 1, 1));

        Assert.Contains("foreigntotal", sql);
        Assert.DoesNotContain("LEFT JOIN vendor", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenSalesOrderLines_UsesSalesOrderTable()
    {
        var sql = NetSuiteSuiteQlQueries.OpenSalesOrderLines(new DateOnly(2026, 1, 1));

        Assert.Contains("INNER JOIN salesorder", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("so.tranDate", sql);
        Assert.Contains("so.entity", sql);
        Assert.DoesNotContain("type = 'SalesOrd'", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenPurchaseOrderLines_UsesPurchaseOrderTable()
    {
        var sql = NetSuiteSuiteQlQueries.OpenPurchaseOrderLines(new DateOnly(2026, 1, 1));

        Assert.Contains("INNER JOIN purchaseorder", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("po.tranDate", sql);
        Assert.Contains("po.entity", sql);
        Assert.DoesNotContain("type = 'PurchOrd'", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StockLocations_AliasesQuantityOnHandAsQuantity()
    {
        var sql = NetSuiteSuiteQlQueries.StockLocations();

        Assert.Contains("quantityonhand AS quantity", sql);
        Assert.Contains("inventorybalance", sql);
    }
}
