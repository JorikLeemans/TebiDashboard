using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Options;

namespace TebiDashboard.Infrastructure.Data;

/// <summary>
/// Runs database migrations after the HTTP server starts so Railway health checks pass quickly.
/// </summary>
public sealed class DatabaseMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.Equals(options.Value.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        _ = RunMigrationsAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var dbContext = scope.ServiceProvider.GetRequiredService<TebiDashboardDbContext>();
            var dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<TebiDashboardDbContext>>();

            logger.LogInformation("Starting database initialization ({Provider}).", dbOptions.Provider);

            if (dbOptions.ApplyMigrationsOnStartup)
            {
                if (IsSqlite(dbContext))
                {
                    await SqliteMigrationHelper.ClearStaleMigrationLockAsync(dbContext, dbLogger, cancellationToken);
                    await SqliteMigrationHelper.BaselineExistingSchemaIfNeededAsync(dbContext, dbLogger, cancellationToken);
                }

                await dbContext.Database.MigrateAsync(cancellationToken);
                dbLogger.LogInformation("Database migrations applied ({Provider}).", dbOptions.Provider);
            }

            if (dbOptions.SeedDemoData)
            {
                await DemoDataSeeder.SeedAsync(dbContext, forceReseed: false, cancellationToken);
                dbLogger.LogInformation("Demo data seed completed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization failed during startup.");
            lifetime.StopApplication();
        }
    }

    private static bool IsSqlite(TebiDashboardDbContext dbContext) =>
        dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
}
