using TebiDashboard.Application.Interfaces;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// No-op demo data service used when the API runs in in-memory test mode.
/// </summary>
public sealed class NullDemoDataAdminService : IDemoDataAdminService
{
    public bool IsAvailable => false;

    public Task ReseedAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
