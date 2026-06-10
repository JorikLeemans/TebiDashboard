namespace TebbiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Open sales or transfer order line for an inventory item.
/// </summary>
public sealed class OpenTransactionRecord
{
    public string TransactionId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public decimal QuantityOpen { get; set; }

    public string Status { get; set; } = string.Empty;
}
