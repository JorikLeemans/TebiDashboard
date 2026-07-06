using TebiDashboard.UI.Services;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Keeps a page subscribed to the same period-filter instance as the layout bar.
/// </summary>
public sealed class PeriodFilterSubscription : IDisposable
{
    private IPeriodFilterService? _service;
    private Action? _handler;

    /// <summary>
    /// Subscribes to period changes; rebinds when the cascaded service instance changes.
    /// </summary>
    public void Bind(IPeriodFilterService? cascaded, IPeriodFilterService injected, Action handler)
    {
        var service = cascaded ?? injected;

        if (ReferenceEquals(_service, service) && ReferenceEquals(_handler, handler))
        {
            return;
        }

        Unbind();
        _service = service;
        _handler = handler;
        _service.OnChanged += handler;
    }

    public void Unbind()
    {
        if (_service is not null && _handler is not null)
        {
            _service.OnChanged -= _handler;
        }

        _service = null;
        _handler = null;
    }

    public void Dispose() => Unbind();
}
