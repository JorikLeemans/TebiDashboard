namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Full customer view with revenue history and linked sales orders for a period.
/// </summary>
public sealed class CustomerDetailDto
{
    public CustomerDto Customer { get; init; } = new();

    public DateOnly PeriodFrom { get; init; }

    public DateOnly PeriodTo { get; init; }

    public decimal TotalSpent { get; init; }

    public IReadOnlyList<RevenueDto> Revenue { get; init; } = [];

    public IReadOnlyList<OpenTransactionDto> SalesOrders { get; init; } = [];
}
