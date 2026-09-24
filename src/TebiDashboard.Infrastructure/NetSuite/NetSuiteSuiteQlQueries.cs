namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// SuiteQL statements used to populate dashboard tables from NetSuite.
/// Field names follow the TEBI Records Browser mapping where SuiteQL supports them.
/// </summary>
internal static class NetSuiteSuiteQlQueries
{
    /// <summary>
    /// Customer: id, entityId (code), glommedName, email, city, and category labels.
    /// glommedName is the display name for both companies and individuals; companyName is empty for persons.
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
            c.glommedName,
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
        ORDER BY c.glommedName
        """;

    /// <summary>
    /// Vendor: id, entityId (code), companyName, email, and city from the default billing address book.
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
    /// Invoice line revenue (omzet): one row per item line so the dashboard can show the
    /// article name and artikelgroep (item class), not the numeric subsidiary internal id.
    /// Header-only invoice.subsidiary was previously stored as productgroupid, which is why
    /// both Item and Product group rendered as "16".
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
            NVL(tl.amount, 0) AS amount
        FROM transactionline tl
        INNER JOIN invoice i ON i.id = tl.transaction
        LEFT JOIN item it ON it.id = tl.item
        WHERE tl.mainline = 'F'
          AND i.tranDate >= TO_DATE('{fromDate:yyyy-MM-dd}', 'YYYY-MM-DD')
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
