namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// SuiteQL statements used to populate dashboard tables from NetSuite.
/// Field names follow the TEBI Records Browser mapping where SuiteQL supports them.
/// </summary>
internal static class NetSuiteSuiteQlQueries
{
    /// <summary>
    /// Customer: entityid (code), companyname, email; internal id for FK joins.
    /// </summary>
    public static string Customers() =>
        """
        SELECT DISTINCT id, entityid, companyname, email
        FROM customer
        WHERE isinactive = 'F'
        ORDER BY companyname
        """;

    /// <summary>
    /// Vendor: entityid (code), companyname, email; internal id used as supplier key for bill joins.
    /// </summary>
    public static string Vendors() =>
        """
        SELECT DISTINCT id, entityid, companyname, email
        FROM vendor
        WHERE isinactive = 'F'
        ORDER BY companyname
        """;

    /// <summary>
    /// Item master: internal id, itemid (SKU/name), upccode (barcode). Reorder point mapped later.
    /// </summary>
    public static string InventoryItems() =>
        """
        SELECT
            id,
            itemid,
            upccode
        FROM item
        WHERE isinactive = 'F'
          AND itemtype IN ('InvtPart', 'Assembly')
        ORDER BY itemid
        """;

    /// <summary>
    /// Stock per item and location. TEBI maps inventorydetail (item, location, quantity);
    /// SuiteQL exposes on-hand stock via inventorybalance (quantityonhand aliased as quantity).
    /// </summary>
    public static string StockLocations() =>
        """
        SELECT
            ib.item,
            ib.location,
            ib.quantityonhand AS quantity
        FROM inventorybalance ib
        WHERE ib.quantityonhand > 0
        ORDER BY ib.item, ib.location
        """;

    /// <summary>
    /// Invoice revenue (omzet bron): trandate, subsidiary, customer entity.
    /// TEBI subtotal is not queryable on transaction; sum non-tax line amounts for excl. BTW.
    /// </summary>
    public static string Invoices(DateOnly fromDate) =>
        $"""
        SELECT
            t.id,
            t.trandate,
            t.entity AS customerid,
            t.subsidiary AS productgroupid,
            NVL(SUM(tl.amount), 0) AS amount
        FROM transaction t
        INNER JOIN transactionline tl ON tl.transaction = t.id
        WHERE t.type = 'CustInvc'
          AND tl.mainline = 'F'
          AND tl.taxline = 'F'
          AND t.trandate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        GROUP BY t.id, t.trandate, t.entity, t.subsidiary
        ORDER BY t.trandate DESC
        """;

    /// <summary>
    /// Vendor bill expenses: trandate, vendor entity, foreigntotal (transaction.total is not SuiteQL-safe).
    /// </summary>
    public static string VendorBills(DateOnly fromDate) =>
        $"""
        SELECT
            t.id,
            t.trandate,
            NVL(t.foreigntotal, 0) AS amount,
            t.entity AS supplierid
        FROM transaction t
        WHERE t.type = 'VendBill'
          AND t.trandate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        ORDER BY t.trandate DESC
        """;

    /// <summary>
    /// Open sales order lines: header entity (customer), line item, open quantity, line amount.
    /// </summary>
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

    /// <summary>
    /// Open purchase order lines: header entity (vendor), line item, open quantity, line amount.
    /// </summary>
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

    /// <summary>
    /// Recent sold quantity per item from posted invoice lines (supports low-stock velocity KPI).
    /// </summary>
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
