using TebiDashboard.Application.Interfaces;
using TebiDashboard.Infrastructure.Data;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// Reseeds dashboard demo rows in the persistent database.
/// </summary>
public sealed class DatabaseDemoDataAdminService(TebiDashboardDbContext dbContext) : IDemoDataAdminService
{
    public bool IsAvailable => true;

    public Task ReseedAsync(CancellationToken cancellationToken = default) =>
        DemoDataSeeder.SeedAsync(dbContext, forceReseed: true, cancellationToken);
}
