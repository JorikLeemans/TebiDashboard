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

    public string Status { get; set; } = string.Empty;
}
