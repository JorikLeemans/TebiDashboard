namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Cancels in-flight period reloads when the user changes the date range again.
/// </summary>
public sealed class PeriodFilterReload
{
    private CancellationTokenSource? _cts;
    private int _generation;

    /// <summary>
    /// Starts a new reload generation and returns its id plus a cancellation token.
    /// </summary>
    public int BeginReload(out CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _generation++;
        cancellationToken = _cts.Token;
        return _generation;
    }

    /// <summary>
    /// Returns false when a newer period change superseded this reload.
    /// </summary>
    public bool IsCurrent(int generation) => generation == _generation;

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
