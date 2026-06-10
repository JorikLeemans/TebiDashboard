using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TebbiDashboard.Infrastructure.Data;

/// <summary>
/// SQLite-only helpers for local development migration edge cases.
/// </summary>
internal static class SqliteMigrationHelper
{
    /// <summary>
    /// Removes a leftover migration lock so startup does not retry forever.
    /// </summary>
    public static async Task ClearStaleMigrationLockAsync(
        TebbiDashboardDbContext dbContext,
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

    /// <summary>
    /// When a local SQLite file already has tables but migration history is outdated
    /// (for example after regenerating InitialCreate for PostgreSQL), stamp pending migrations
    /// instead of failing with "table already exists".
    /// </summary>
    public static async Task BaselineExistingSchemaIfNeededAsync(
        TebbiDashboardDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        // Only recover when the app schema is already present from an earlier local migration run.
        if (!await TableExistsAsync(dbContext, "Users", cancellationToken)
            || !await TableExistsAsync(dbContext, "InventoryItems", cancellationToken))
        {
            return;
        }

        var productVersion = await GetKnownProductVersionAsync(dbContext, cancellationToken);

        foreach (var migrationId in pending)
        {
            logger.LogWarning(
                "SQLite schema already exists. Baselines migration '{MigrationId}' for local development.",
                migrationId);

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ({0}, {1});
                """,
                [migrationId, productVersion],
                cancellationToken);
        }
    }

    private static async Task<string> GetKnownProductVersionAsync(
        TebbiDashboardDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var version = await dbContext.Database
                .SqlQueryRaw<string>("SELECT \"ProductVersion\" FROM \"__EFMigrationsHistory\" LIMIT 1")
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(version))
            {
                return version;
            }
        }
        catch (Exception)
        {
            // History table may not exist yet on a brand-new database.
        }

        return "10.0.9";
    }

    private static async Task<bool> TableExistsAsync(
        TebbiDashboardDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
