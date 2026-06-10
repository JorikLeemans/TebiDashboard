namespace TebiDashboard.Shared.Filters;

/// <summary>
/// Shared date range filter for dashboard queries.
/// </summary>
public sealed class DateRangeFilter
{
    public DateOnly From { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
    public DateOnly To { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
