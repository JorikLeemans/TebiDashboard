namespace TebiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Open sales or transfer order line for an inventory item.
/// </summary>
public sealed class OpenTransactionRecord
{
    public string TransactionId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Item display name from NetSuite (BUILTIN.DF) or demo seed. Used when the
    /// inventory master row is missing so the UI does not show a raw internal id.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Linked customer for sales orders; null for transfer/purchase orders.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Linked supplier for purchase orders; null for sales/transfer orders.
    /// </summary>
    public string? SupplierId { get; set; }

    /// <summary>
    /// Date the transaction was created or last updated (used for period filtering).
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    public decimal QuantityOpen { get; set; }

    /// <summary>
    /// Monetary value of the open order line (sales or purchase orders).
    /// </summary>
    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;
}
