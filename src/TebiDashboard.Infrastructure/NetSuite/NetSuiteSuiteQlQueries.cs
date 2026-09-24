namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// SuiteQL statements used to populate dashboard tables from NetSuite.
/// Field names follow the TEBI Records Browser mapping where SuiteQL supports them.
/// </summary>
internal static class NetSuiteSuiteQlQueries
{
    /// <summary>
    /// Customer: id, entityId (code), companyName, email, defaultBillingAddress (TEBI SuiteQL field names).
    /// custentity_tebi_primaire_dochtermaatsch is available but not stored until subsidiary filtering is added.
    /// </summary>
    public static string Customers() =>
        """
        SELECT DISTINCT
            id,
            entityId,
            companyName,
            email,
            defaultBillingAddress
        FROM customer
        WHERE isInactive = 'F'
        ORDER BY companyName
        """;

    /// <summary>
    /// Vendor: id, entityId (code), companyName, email, defaultBillingAddress (TEBI SuiteQL field names).
    /// </summary>
    public static string Vendors() =>
        """
        SELECT DISTINCT
            id,
            entityId,
            companyName,
            email,
            defaultBillingAddress
        FROM vendor
        WHERE isInactive = 'F'
        ORDER BY companyName
        """;

    /// <summary>
    /// Item master: id, itemId (SKU), upcCode (barcode). reorderPoint exists in Records Browser but not in SuiteQL item.
    /// </summary>
    public static string InventoryItems() =>
        """
        SELECT
            id,
            itemId,
            displayName,
            upcCode
        FROM item
        WHERE isInactive = 'F'
          AND itemType IN ('InvtPart', 'Assembly')
        ORDER BY itemId
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
    /// Invoice revenue (omzet bron): tranId, tranDate, entity, subsidiary; amount excl. BTW via total - taxTotal.
    /// Uses the Invoice SuiteQL table (TEBI field names) instead of generic transaction joins.
    /// productgroupname resolves the subsidiary reference to its display name via BUILTIN.DF so the
    /// dashboard shows a readable product group instead of the raw numeric id.
    /// </summary>
    public static string Invoices(DateOnly fromDate) =>
        $"""
        SELECT
            id,
            tranId,
            tranDate,
            entity AS customerid,
            subsidiary AS productgroupid,
            BUILTIN.DF(subsidiary) AS productgroupname,
            NVL(total, 0) - NVL(taxTotal, 0) AS amount
        FROM invoice
        WHERE tranDate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        ORDER BY tranDate DESC
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
    /// Open sales order lines: salesOrder header + transactionline details (TEBI SuiteQL field names).
    /// </summary>
    public static string OpenSalesOrderLines(DateOnly fromDate) =>
        $"""
        SELECT
            tl.transaction,
            tl.id AS lineid,
            so.tranId,
            so.tranDate,
            so.entity AS customerid,
            tl.item AS itemid,
            (tl.quantity - NVL(tl.quantityshiprecv, 0)) AS quantityopen,
            NVL(tl.amount, 0) AS amount,
            so.status AS status
        FROM transactionline tl
        INNER JOIN salesorder so ON so.id = tl.transaction
        WHERE tl.mainline = 'F'
          AND tl.quantity > NVL(tl.quantityshiprecv, 0)
          AND so.tranDate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        ORDER BY so.tranDate DESC
        """;

    /// <summary>
    /// Open purchase order lines: purchaseOrder header + transactionline details (TEBI SuiteQL field names).
    /// </summary>
    public static string OpenPurchaseOrderLines(DateOnly fromDate) =>
        $"""
        SELECT
            tl.transaction,
            tl.id AS lineid,
            po.tranId,
            po.tranDate,
            po.entity AS supplierid,
            tl.item AS itemid,
            (tl.quantity - NVL(tl.quantityshiprecv, 0)) AS quantityopen,
            NVL(tl.amount, 0) AS amount,
            po.status AS status
        FROM transactionline tl
        INNER JOIN purchaseorder po ON po.id = tl.transaction
        WHERE tl.mainline = 'F'
          AND tl.quantity > NVL(tl.quantityshiprecv, 0)
          AND po.tranDate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        ORDER BY po.tranDate DESC
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
        INNER JOIN invoice i ON i.id = tl.transaction
        WHERE tl.mainline = 'F'
          AND i.tranDate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
        GROUP BY tl.item
        """;
}
