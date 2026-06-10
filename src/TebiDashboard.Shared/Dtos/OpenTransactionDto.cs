namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Open sales or transfer transaction for an item.
/// </summary>
public sealed class OpenTransactionDto
{
    public string TransactionId { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string ItemId { get; init; } = string.Empty;

    public string ItemName { get; init; } = string.Empty;

    public DateOnly TransactionDate { get; init; }

    public decimal QuantityOpen { get; init; }

    public string Status { get; init; } = string.Empty;
}
