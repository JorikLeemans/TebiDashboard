namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Revenue and expenses for one equal-length window in a period comparison.
/// </summary>
public sealed class PeriodTrendPointDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public decimal Revenue { get; init; }

    public decimal Expenses { get; init; }
}
