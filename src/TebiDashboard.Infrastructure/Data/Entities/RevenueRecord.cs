namespace TebiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Revenue line persisted for dashboard reporting.
/// </summary>
public sealed class RevenueRecord
{
    public string Id { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public decimal Amount { get; set; }

    /// <summary>
    /// Units sold on this invoice line. SuiteQL <c>transactionline.quantity</c> is negative
    /// when the line reduces inventory, so the sync stores the positive quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// True after a sync has written <see cref="Quantity"/> from SuiteQL.
    /// Older rows stay false so the next sync re-reads invoices outside the short lookback.
    /// </summary>
    public bool UnitsCaptured { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string ProductGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable product group (artikelgroep) name shown in the UI.
    /// <see cref="ProductGroupId"/> stays the NetSuite internal id. When this is empty the UI
    /// falls back to that id so older rows still render.
    /// </summary>
    public string ProductGroupName { get; set; } = string.Empty;

    /// <summary>
    /// Inventory item internal id from the invoice line. Empty on header-only legacy rows
    /// that stored a subsidiary id as the product group.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Display name captured at sync time (SKU / displayName / BUILTIN.DF). The read path
    /// prefers a live inventory lookup and uses this when that row is missing.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Parent invoice internal id. Line rows use "{invoiceId}-{lineId}" as <see cref="Id"/>.
    /// </summary>
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Parent invoice document number (NetSuite tranId).
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;
}
