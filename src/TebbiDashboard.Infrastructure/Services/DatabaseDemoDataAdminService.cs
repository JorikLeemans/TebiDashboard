using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Infrastructure.Data;

namespace TebbiDashboard.Infrastructure.Services;

/// <summary>
/// Reseeds dashboard demo rows in the persistent database.
/// </summary>
public sealed class DatabaseDemoDataAdminService(TebbiDashboardDbContext dbContext) : IDemoDataAdminService
{
    public bool IsAvailable => true;

    public Task ReseedAsync(CancellationToken cancellationToken = default) =>
        DemoDataSeeder.SeedAsync(dbContext, forceReseed: true, cancellationToken);
}
