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

        return configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tebbi.db";
    }

    public static bool UsesPostgres(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(GetDatabaseUrl(configuration))
        || string.Equals(
            configuration.GetSection("Database")["Provider"],
            "PostgreSql",
            StringComparison.OrdinalIgnoreCase);

    private static string? GetDatabaseUrl(IConfiguration configuration) =>
        configuration["DATABASE_URL"] ?? Environment.GetEnvironmentVariable("DATABASE_URL");

    private static string ToNpgsqlConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
