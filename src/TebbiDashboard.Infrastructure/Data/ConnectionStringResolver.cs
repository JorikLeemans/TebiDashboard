using Microsoft.Extensions.Configuration;

namespace TebbiDashboard.Infrastructure.Data;

/// <summary>
/// Builds Npgsql connection strings from Railway/Heroku-style DATABASE_URL values and local config.
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

        return configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=tebbidashboard;Username=tebbi;Password=tebbi_dev_password";
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

        var connectionString =
            $"Host={host};Port={port};Database={database};Username={username};Password={password}";

        return AppendSslIfRemote(connectionString, host);
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

        var connectionString =
            $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password}";

        return AppendSslIfRemote(connectionString, uri.Host);
    }

    /// <summary>
    /// Railway requires TLS; local Docker PostgreSQL does not.
    /// </summary>
    private static string AppendSslIfRemote(string connectionString, string host)
    {
        if (IsLocalHost(host))
        {
            return connectionString;
        }

        return $"{connectionString};SSL Mode=Require;Trust Server Certificate=true";
    }

    private static bool IsLocalHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("postgres", StringComparison.OrdinalIgnoreCase);
}
