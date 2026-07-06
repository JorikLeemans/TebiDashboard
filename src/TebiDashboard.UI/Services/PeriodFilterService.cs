namespace TebiDashboard.UI.Services;

/// <summary>
/// Stores the active dashboard period in memory for the current user session.
/// </summary>
public sealed class PeriodFilterService : IPeriodFilterService
{
    // Recompute each time so presets stay aligned with the current calendar day.
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly From { get; private set; } = Today.AddDays(-2);

    public DateOnly To { get; private set; } = Today;

    public bool IsExpanded { get; private set; } = true;

    public event Action? OnChanged;

    public void ApplyPreset(PeriodFilterPreset preset)
    {
        (From, To) = preset switch
        {
            PeriodFilterPreset.Last2Days => (Today.AddDays(-2), Today),
            PeriodFilterPreset.Last7Days => (Today.AddDays(-7), Today),
            PeriodFilterPreset.Last30Days => (Today.AddDays(-30), Today),
            PeriodFilterPreset.ThisMonth => (new DateOnly(Today.Year, Today.Month, 1), Today),
            PeriodFilterPreset.ThisYear => (new DateOnly(Today.Year, 1, 1), Today),
            _ => (Today.AddDays(-2), Today)
        };

        OnChanged?.Invoke();
    }

    public void SetCustomRange(DateOnly from, DateOnly to)
    {
        From = from <= to ? from : to;
        To = from <= to ? to : from;
        OnChanged?.Invoke();
    }

    public void SetExpanded(bool expanded)
    {
        if (IsExpanded == expanded)
        {
            return;
        }

        IsExpanded = expanded;
        // UI expand/collapse should not trigger data reloads on dashboard pages.
    }
}
