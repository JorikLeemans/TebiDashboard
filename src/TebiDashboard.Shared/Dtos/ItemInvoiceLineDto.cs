namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// One invoice line that contributes to an item's sold-in-period amount.
/// </summary>
public sealed class ItemInvoiceLineDto
{
    public DateOnly Date { get; init; }

    public string InvoiceId { get; init; } = string.Empty;

    /// <summary>Invoice document number shown to operators. Falls back to <see cref="InvoiceId"/>.</summary>
    public string InvoiceNumber { get; init; } = string.Empty;

    public string CustomerId { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    /// <summary>Invoice line amount in euros.</summary>
    public decimal Amount { get; init; }

    /// <summary>Units on this invoice line.</summary>
    public decimal UnitsSold { get; init; }
}
