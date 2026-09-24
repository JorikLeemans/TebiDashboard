namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Company expense record for finance overview.
/// </summary>
public sealed class ExpenseDto
{
    public string Id { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string Category { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? VendorName { get; init; }

    public string? SupplierId { get; init; }

    /// <summary>
    /// Supplier master name for <see cref="SupplierId"/>.
    /// The supplier id itself when the master row cannot be resolved, or empty when the expense has no supplier.
    /// </summary>
    public string SupplierName { get; init; } = string.Empty;
}
