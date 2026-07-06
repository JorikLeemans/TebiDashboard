namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Ignores stale period reload responses when the user changes the range quickly.
/// </summary>
public sealed class PeriodFilterReload
{
    private int _generation;

    /// <summary>
    /// Starts a new reload generation.
    /// </summary>
    public int BeginReload() => ++_generation;

    /// <summary>
    /// Returns false when a newer period change superseded this reload.
    /// </summary>
    public bool IsCurrent(int generation) => generation == _generation;
}
