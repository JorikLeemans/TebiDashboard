using Microsoft.Extensions.Configuration;

namespace TebbiDashboard.Infrastructure.Data;

/// <summary>
/// Builds Npgsql connection strings from Railway/Heroku-style DATABASE_URL values.
/// </summary>
internal static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var postgresUrl = GetDatabaseUrl(configuration);
        if (!string.IsNullOrWhiteSpace(postgresUrl))
        {
            return ToNpgsqlConnectionString(postgresUrl);
        }

        var fromEnv = BuildFromPostgresEnvironmentVariables();
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        return configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tebbi.db";
    }

    public static bool UsesPostgres(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(GetDatabaseUrl(configuration))
        || HasPostgresEnvironmentVariables()
        || string.Equals(
            configuration.GetSection("Database")["Provider"],
            "PostgreSql",
            StringComparison.OrdinalIgnoreCase);

    private static bool HasPostgresEnvironmentVariables() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PGHOST"));

    private static string? GetDatabaseUrl(IConfiguration configuration) =>
        configuration["DATABASE_URL"] ?? Environment.GetEnvironmentVariable("DATABASE_URL");

    private static string? BuildFromPostgresEnvironmentVariables()
    {
        var host = Environment.GetEnvironmentVariable("PGHOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var port = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("PGDATABASE") ?? "railway";
        var username = Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? string.Empty;

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }

    private static string ToNpgsqlConnectionString(string databaseUrl)
    {
        // Normalize postgres:// to postgresql:// for reliable Uri parsing.
        var normalized = databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            ? string.Concat("postgresql://", databaseUrl.AsSpan("postgres://".Length))
            : databaseUrl;

        var uri = new Uri(normalized);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
