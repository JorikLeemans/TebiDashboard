namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// SuiteQL statements used to populate dashboard tables from NetSuite.
/// </summary>
internal static class NetSuiteSuiteQlQueries
{
    public static string Customers() =>
        """
        SELECT id, companyname, email
        FROM customer
        WHERE isinactive = 'F'
        ORDER BY companyname
        """;

    public static string Vendors() =>
        """
        SELECT id, companyname, email
        FROM vendor
        WHERE isinactive = 'F'
        ORDER BY companyname
        """;

    public static string InventoryItems() =>
        """
        SELECT
            i.id,
            i.displayname,
            i.upccode,
            NVL(i.reorderpoint, 0) AS reorderpoint,
            NVL(SUM(ib.quantityonhand), 0) AS quantityonhand
        FROM item i
        LEFT JOIN inventorybalance ib ON ib.item = i.id
        WHERE i.isinactive = 'F'
          AND i.itemtype IN ('InvtPart', 'Assembly')
        GROUP BY i.id, i.displayname, i.upccode, i.reorderpoint
        ORDER BY i.displayname
        """;

    public static string StockLocations() =>
        """
        SELECT
            ib.item,
            ib.location,
            l.name AS locationname,
            ib.quantityonhand
        FROM inventorybalance ib
        INNER JOIN location l ON l.id = ib.location
        WHERE ib.quantityonhand > 0
        ORDER BY ib.item, l.name
        """;

    public static string Invoices(DateOnly fromDate) =>
        $"""
        SELECT
            t.id,
            t.trandate,
            NVL(t.foreigntotal, 0) AS amount,
            t.entity AS customerid,
            c.companyname AS customername,
            NVL(TO_CHAR(t.class), '') AS productgroupid
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
            BUILTIN.DF(t.status) AS status
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
            (tl.quantity - NVL(tl.quantityreceived, 0)) AS quantityopen,
            NVL(tl.amount, 0) AS amount,
            BUILTIN.DF(t.status) AS status
        FROM transactionline tl
        INNER JOIN transaction t ON t.id = tl.transaction
        WHERE t.type = 'PurchOrd'
          AND tl.mainline = 'F'
          AND tl.quantity > NVL(tl.quantityreceived, 0)
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
