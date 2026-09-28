namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Hover legend text for dashboard status columns.
/// </summary>
public enum StatusLegendKind
{
    /// <summary>Inventory stock health (OK / Low stock).</summary>
    InventoryStock,

    /// <summary>Open sales order lines on customer views.</summary>
    SalesOrder,

    /// <summary>Open purchase order lines on supplier views.</summary>
    PurchaseOrder,

    /// <summary>Mixed open transactions (sales, purchase, transfer).</summary>
    OpenTransaction
}

/// <summary>
/// Tooltip descriptions shown when hovering a Status column header.
/// </summary>
public static class StatusLegends
{
    public static string GetTooltip(StatusLegendKind kind) => kind switch
    {
        StatusLegendKind.InventoryStock =>
            "OK — every location is above its own reorder point. " +
            "Low stock — at least one location's on-hand quantity is at or below that location's reorder point.",

        StatusLegendKind.SalesOrder =>
            "Pending Fulfillment — order confirmed; items still need to be shipped. " +
            "Pending Billing — items shipped; invoice not yet created. " +
            "Open — order is still being processed in NetSuite.",

        StatusLegendKind.PurchaseOrder =>
            "Pending Receipt — items ordered but not fully received yet. " +
            "Pending Billing — received; vendor bill not yet created. " +
            "Open — order is still being processed in NetSuite.",

        StatusLegendKind.OpenTransaction =>
            "Pending Fulfillment — open sales order line awaiting shipment. " +
            "Pending Receipt — open purchase or transfer line awaiting receipt. " +
            "Open — other open order status from NetSuite.",

        _ => string.Empty
    };
}
