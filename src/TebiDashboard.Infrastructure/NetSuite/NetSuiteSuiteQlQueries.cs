namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// SuiteQL statements used to populate dashboard tables from NetSuite.
/// Field names follow the TEBI Records Browser mapping where SuiteQL supports them.
/// </summary>
internal static class NetSuiteSuiteQlQueries
{
    /// <summary>
    /// Customer: id, entityId (auto-number code), isPerson, glommedName, companyName, email, city, categories.
    /// Records Browser: entityId is the Customer ID (K###### when Auto-Generated Numbering is on);
    /// companyName is the legal company name; isPerson switches Company vs Individual.
    /// https://system.netsuite.com/help/helpcenter/en_US/srbrowser/Browser2026_1/schema/record/customer.html
    /// glommedName is first+middle+last for Individual customers (SuiteQL; not in the Records Browser).
    /// City comes from the default billing address book row. defaultAddress is not exposed to SuiteQL SEARCH
    /// (NOT_EXPOSED). Analytics Browser join: customerAddressBook.entity + customerAddressBookEntityAddress.nkey.
    /// category and custentity_tebi_klant_subcategorie are list fields; BUILTIN.DF returns the text, not the id.
    /// custentity_tebi_primaire_dochtermaatsch is available but not stored until subsidiary filtering is added.
    /// </summary>
    public static string Customers() =>
        """
        SELECT DISTINCT
            c.id,
            c.entityId,
            c.isPerson,
            c.glommedName,
            c.companyName,
            c.email,
            ea.city,
            BUILTIN.DF(c.category) AS category,
            BUILTIN.DF(c.custentity_tebi_klant_subcategorie) AS subcategory
        FROM customer c
        LEFT JOIN customerAddressBook cab
            ON cab.entity = c.id
           AND cab.defaultBilling = 'T'
        LEFT JOIN customerAddressBookEntityAddress ea
            ON ea.nKey = cab.addressBookAddress
        WHERE c.isInactive = 'F'
        ORDER BY c.entityId
        """;

    /// <summary>
    /// Vendor: id, entityId (code), companyName, email, and city from the default billing address book.
    /// The stored name is entityId plus companyName, the same pairing as a company customer.
    /// Same address-book path as customers so the City column stores a town, not the full address.
    /// </summary>
    public static string Vendors() =>
        """
        SELECT DISTINCT
            v.id,
            v.entityId,
            v.companyName,
            v.email,
            ea.city
        FROM vendor v
        LEFT JOIN vendorAddressBook vab
            ON vab.entity = v.id
           AND vab.defaultBilling = 'T'
        LEFT JOIN vendorAddressBookEntityAddress ea
            ON ea.nKey = vab.addressBookAddress
        WHERE v.isInactive = 'F'
        ORDER BY v.companyName
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
    /// ib.location is the internal id; BUILTIN.DF returns the location name without joining location.
    /// Oracle: https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/article_1029114613.html
    /// </summary>
    public static string StockLocations() =>
        """
        SELECT
            ib.item,
            ib.location,
            BUILTIN.DF(ib.location) AS locationname,
            ib.quantityonhand AS quantity
        FROM inventorybalance ib
        WHERE ib.quantityonhand > 0
        ORDER BY ib.item, ib.location
        """;

    /// <summary>
    /// Per-item, per-location reorder point and preferred stock.
    /// Record id itemlocationconfiguration; fields reorderpoint and preferredstocklevel.
    /// Oracle: https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_1504729157.html
    /// Records Browser: itemlocationconfiguration.reorderpoint / preferredstocklevel.
    /// Requires Multi-Location Inventory, Advanced Inventory Management, and Advanced Item Location Configuration.
    /// </summary>
    public static string ItemLocationConfigurations() =>
        """
        SELECT
            ilc.item,
            ilc.location,
            BUILTIN.DF(ilc.location) AS locationname,
            NVL(ilc.reorderpoint, 0) AS reorderpoint,
            NVL(ilc.preferredstocklevel, 0) AS preferredstocklevel
        FROM itemlocationconfiguration ilc
        ORDER BY ilc.item, ilc.location
        """;

    /// <summary>
    /// Invoice line revenue (omzet): one row per item line so the dashboard can show the
    /// article name and artikelgroep (item class), not the numeric subsidiary internal id.
    /// Header-only invoice.subsidiary was previously stored as productgroupid, which is why
    /// both Item and Product group rendered as "16".
    /// NetSuite stores invoice item lines as credits (debit-positive / credit-negative), so
    /// tl.amount is negative when the customer owes us. Negate it for customer-facing omzet.
    /// transactionline.quantity is negative when the line reduces inventory (invoice, item fulfillment).
    /// Multiply by -1 so units sold are positive. A distinct alias avoids clashing with the raw column.
    /// Records Browser: transactionline.quantity (float).
    /// https://system.netsuite.com/help/helpcenter/en_US/srbrowser/Browser2021_1/analytics/record/transactionLine.html
    /// Invoice quantity sign: https://devblog.mirerp.com/posts/transactions-inventory/
    /// Oracle example uses quantity * -1:
    /// https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_158039627694.html
    /// fromDate is the Admin lookback, or earlier when stored lines still have no units.
    /// REST SuiteQL pages this result (limit/offset, max 1000 rows per page).
    /// </summary>
    public static string Invoices(DateOnly fromDate) =>
        $"""
        SELECT
            i.id AS invoiceid,
            i.tranId AS invoicenumber,
            tl.id AS lineid,
            TO_CHAR(i.tranDate, 'YYYY-MM-DD') AS trandate,
            i.entity AS customerid,
            tl.item AS itemid,
            BUILTIN.DF(tl.item) AS itemname,
            it.class AS productgroupid,
            BUILTIN.DF(it.class) AS productgroupname,
            -NVL(tl.amount, 0) AS amount,
            NVL(tl.quantity, 0) * -1 AS soldquantity
        FROM transactionline tl
        INNER JOIN invoice i ON i.id = tl.transaction
        LEFT JOIN item it ON it.id = tl.item
        WHERE tl.mainline = 'F'
          AND i.tranDate >= TO_DATE('{SuiteQlDate(fromDate)}', 'YYYY-MM-DD')
        ORDER BY i.tranDate DESC
        """;

    /// <summary>
    /// Vendor bill expenses: trandate, vendor entity, foreigntotal (transaction.total is not SuiteQL-safe).
    /// The date is formatted as YYYY-MM-DD for the same reason as invoice tranDate.
    /// </summary>
    public static string VendorBills(DateOnly fromDate) =>
        $"""
        SELECT
            t.id,
            TO_CHAR(t.trandate, 'YYYY-MM-DD') AS trandate,
            NVL(t.foreigntotal, 0) AS amount,
            t.entity AS supplierid
        FROM transaction t
        WHERE t.type = 'VendBill'
          AND t.trandate >= TO_DATE('{SuiteQlDate(fromDate)}', 'YYYY-MM-DD')
        ORDER BY t.trandate DESC
        """;

    /// <summary>
    /// Open sales order lines: every non-closed order, not only those inside the invoice lookback.
    /// Status H/C (and SalesOrd:H/C) are Closed/Cancelled. Remaining quantity must still be open.
    /// </summary>
    public static string OpenSalesOrderLines() =>
        """
        SELECT
            tl.transaction,
            tl.id AS lineid,
            so.tranId,
            TO_CHAR(so.tranDate, 'YYYY-MM-DD') AS trandate,
            so.entity AS customerid,
            tl.item AS itemid,
            BUILTIN.DF(tl.item) AS itemname,
            (tl.quantity - NVL(tl.quantityshiprecv, 0)) AS quantityopen,
            NVL(tl.amount, 0) AS amount,
            so.status AS status
        FROM transactionline tl
        INNER JOIN salesorder so ON so.id = tl.transaction
        WHERE tl.mainline = 'F'
          AND tl.quantity > NVL(tl.quantityshiprecv, 0)
          AND so.status NOT IN ('H', 'C', 'SalesOrd:H', 'SalesOrd:C', 'Closed', 'Cancelled')
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
            TO_CHAR(po.tranDate, 'YYYY-MM-DD') AS trandate,
            po.entity AS supplierid,
            tl.item AS itemid,
            BUILTIN.DF(tl.item) AS itemname,
            (tl.quantity - NVL(tl.quantityshiprecv, 0)) AS quantityopen,
            NVL(tl.amount, 0) AS amount,
            po.status AS status
        FROM transactionline tl
        INNER JOIN purchaseorder po ON po.id = tl.transaction
        WHERE tl.mainline = 'F'
          AND tl.quantity > NVL(tl.quantityshiprecv, 0)
          AND po.tranDate >= TO_DATE('{SuiteQlDate(fromDate)}', 'YYYY-MM-DD')
        ORDER BY po.tranDate DESC
        """;

    /// <summary>
    /// Recent sold quantity per item from posted invoice lines (low-stock velocity).
    /// transactionline.quantity is negative when the line reduces inventory.
    /// Multiply by -1, the same sign correction as <see cref="Invoices"/>.
    /// Oracle's SuiteQL example does this: quantity * -1.
    /// https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_158039627694.html
    /// </summary>
    public static string RecentItemSales(DateOnly fromDate) =>
        $"""
        SELECT
            tl.item AS itemid,
            NVL(SUM(NVL(tl.quantity, 0) * -1), 0) AS recentsalesvolume
        FROM transactionline tl
        INNER JOIN invoice i ON i.id = tl.transaction
        WHERE tl.mainline = 'F'
          AND i.tranDate >= TO_DATE('{SuiteQlDate(fromDate)}', 'YYYY-MM-DD')
        GROUP BY tl.item
        """;

    /// <summary>
    /// Formats a date for SuiteQL TO_DATE. The parameter is DateOnly, so a future
    /// free-text filter cannot be concatenated into the query.
    /// </summary>
    private static string SuiteQlDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
