namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Full supplier view with expenses, purchase orders, and purchased items for a period.
/// </summary>
public sealed class SupplierDetailDto
{
    public SupplierDto Supplier { get; init; } = new();

    public DateOnly PeriodFrom { get; init; }

    public DateOnly PeriodTo { get; init; }

    public decimal TotalPurchased { get; init; }

    public IReadOnlyList<ExpenseDto> Expenses { get; init; } = [];

    public IReadOnlyList<OpenTransactionDto> PurchaseOrders { get; init; } = [];

    public IReadOnlyList<SupplierPurchasedItemDto> PurchasedItems { get; init; } = [];
}
