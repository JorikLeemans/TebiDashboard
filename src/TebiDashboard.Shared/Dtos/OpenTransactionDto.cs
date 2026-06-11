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

    public string? CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string? SupplierId { get; init; }

    public string SupplierName { get; init; } = string.Empty;

    public DateOnly TransactionDate { get; init; }

    public decimal QuantityOpen { get; init; }

    /// <summary>
    /// Order line value in euros for sales and purchase orders.
    /// </summary>
    public decimal Amount { get; init; }

    public string Status { get; init; } = string.Empty;
}
