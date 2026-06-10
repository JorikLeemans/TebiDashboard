using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TebbiDashboard.Application.Options;

namespace TebbiDashboard.Infrastructure.Data;

/// <summary>
/// Applies migrations and seeds demo data when the API starts.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TebbiDashboardDbContext>>();

        if (string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<TebbiDashboardDbContext>();

        if (options.ApplyMigrationsOnStartup)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied ({Provider}).", options.Provider);
        }

        if (options.SeedDemoData)
        {
            await DemoDataSeeder.SeedAsync(dbContext, forceReseed: false, cancellationToken);
            logger.LogInformation("Demo data seed completed.");
        }
    }
}
