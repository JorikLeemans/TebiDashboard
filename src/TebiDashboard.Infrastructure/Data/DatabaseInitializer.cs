using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Options;

namespace TebiDashboard.Infrastructure.Data;

/// <summary>
/// Applies migrations and seeds demo data when the API starts.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TebiDashboardDbContext>>();

        if (string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<TebiDashboardDbContext>();

        if (options.ApplyMigrationsOnStartup)
        {
            // A killed API process can leave EF Core's migration lock row behind on SQLite.
            if (IsSqlite(dbContext))
            {
                await ClearStaleSqliteMigrationLockAsync(dbContext, logger, cancellationToken);
            }

            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied ({Provider}).", options.Provider);
        }

        if (options.SeedDemoData)
        {
            await DemoDataSeeder.SeedAsync(dbContext, forceReseed: false, cancellationToken);
            logger.LogInformation("Demo data seed completed.");
        }
    }

    /// <summary>
    /// Removes a leftover migration lock so startup does not retry forever.
    /// </summary>
    private static async Task ClearStaleSqliteMigrationLockAsync(
        TebiDashboardDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await dbContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"__EFMigrationsLock\" WHERE \"Id\" = 1;",
                cancellationToken);

            if (deleted > 0)
            {
                logger.LogWarning(
                    "Removed stale EF Core migration lock from SQLite. " +
                    "This usually means a previous API process was stopped while migrating.");
            }
        }
        catch (Exception ex)
        {
            // Lock table does not exist yet on a fresh database.
            logger.LogDebug(ex, "SQLite migration lock table not present yet.");
        }
    }

    private static bool IsSqlite(TebiDashboardDbContext dbContext) =>
        dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
}
