namespace TebiDashboard.UI.Components;

/// <summary>
/// Single bar entry for the reusable bar chart component.
/// </summary>
public sealed class ChartBarItem
{
    public string Label { get; init; } = string.Empty;

    public decimal Value { get; init; }
}
