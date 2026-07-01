namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// SuiteQL statements used to populate dashboard tables from NetSuite.
/// </summary>
internal static class NetSuiteSuiteQlQueries
{
    public static string Customers() =>
        """
        SELECT DISTINCT id, companyname, email
        FROM customer
        WHERE isinactive = 'F'
        ORDER BY companyname
        """;

    public static string Vendors() =>
        """
        SELECT DISTINCT id, companyname, email
        FROM vendor
        WHERE isinactive = 'F'
        ORDER BY companyname
        """;

    /// <summary>
    /// Item master only; quantities are derived from stock location rows after sync.
    /// </summary>
    public static string InventoryItems() =>
        """
        SELECT
            id,
            displayname,
            upccode,
            NVL(reorderpoint, 0) AS reorderpoint
        FROM item
        WHERE isinactive = 'F'
          AND itemtype IN ('InvtPart', 'Assembly')
        ORDER BY displayname
        """;

    public static string StockLocations() =>
        """
        SELECT
            ib.item,
            ib.location,
            ib.quantityonhand
        FROM inventorybalance ib
        WHERE ib.quantityonhand > 0
        ORDER BY ib.item, ib.location
        """;

    public static string Invoices(DateOnly fromDate) =>
        $"""
        SELECT
            t.id,
            t.trandate,
            NVL(t.foreigntotal, 0) AS amount,
            t.entity AS customerid,
            c.companyname AS customername,
            t.class AS productgroupid
        FROM transaction t
        LEFT JOIN customer c ON c.id = t.entity
        WHERE t.type = 'CustInvc'
          AND t.trandate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        ORDER BY t.trandate DESC
        """;

    public static string VendorBills(DateOnly fromDate) =>
        $"""
        SELECT
            t.id,
            t.trandate,
            NVL(t.foreigntotal, 0) AS amount,
            t.entity AS supplierid,
            v.companyname AS vendorname
        FROM transaction t
        LEFT JOIN vendor v ON v.id = t.entity
        WHERE t.type = 'VendBill'
          AND t.trandate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        ORDER BY t.trandate DESC
        """;

    public static string OpenSalesOrderLines(DateOnly fromDate) =>
        $"""
        SELECT
            tl.transaction,
            tl.id AS lineid,
            t.trandate,
            t.entity AS customerid,
            tl.item AS itemid,
            (tl.quantity - NVL(tl.quantityshiprecv, 0)) AS quantityopen,
            NVL(tl.amount, 0) AS amount,
            t.status AS status
        FROM transactionline tl
        INNER JOIN transaction t ON t.id = tl.transaction
        WHERE t.type = 'SalesOrd'
          AND tl.mainline = 'F'
          AND tl.quantity > NVL(tl.quantityshiprecv, 0)
          AND t.trandate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        ORDER BY t.trandate DESC
        """;

    public static string OpenPurchaseOrderLines(DateOnly fromDate) =>
        $"""
        SELECT
            tl.transaction,
            tl.id AS lineid,
            t.trandate,
            t.entity AS supplierid,
            tl.item AS itemid,
            (tl.quantity - NVL(tl.quantityshiprecv, 0)) AS quantityopen,
            NVL(tl.amount, 0) AS amount,
            t.status AS status
        FROM transactionline tl
        INNER JOIN transaction t ON t.id = tl.transaction
        WHERE t.type = 'PurchOrd'
          AND tl.mainline = 'F'
          AND tl.quantity > NVL(tl.quantityshiprecv, 0)
          AND t.trandate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        ORDER BY t.trandate DESC
        """;

    public static string RecentItemSales(DateOnly fromDate) =>
        $"""
        SELECT
            tl.item AS itemid,
            NVL(SUM(tl.quantity), 0) AS recentsalesvolume
        FROM transactionline tl
        INNER JOIN transaction t ON t.id = tl.transaction
        WHERE t.type = 'CustInvc'
          AND tl.mainline = 'F'
          AND t.trandate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        GROUP BY tl.item
        """;
}
