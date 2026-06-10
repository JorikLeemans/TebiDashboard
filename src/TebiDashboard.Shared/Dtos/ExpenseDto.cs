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
}
