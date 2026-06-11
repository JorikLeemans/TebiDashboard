namespace TebiDashboard.UI.Services;

/// <summary>
/// Shared date-range filter used across dashboard pages.
/// </summary>
public interface IPeriodFilterService
{
    DateOnly From { get; }

    DateOnly To { get; }

    bool IsExpanded { get; }

    event Action? OnChanged;

    void ApplyPreset(PeriodFilterPreset preset);

    void SetCustomRange(DateOnly from, DateOnly to);

    void SetExpanded(bool expanded);
}

/// <summary>
/// Quick period selections for the dashboard filter bar.
/// </summary>
public enum PeriodFilterPreset
{
    Last2Days,
    Last7Days,
    Last30Days,
    ThisMonth,
    ThisYear
}
