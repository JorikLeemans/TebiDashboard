namespace TebiDashboard.Application.Options;

/// <summary>
/// Database provider and startup behaviour for persistent storage.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Sqlite (legacy), PostgreSql (local + hosted), or InMemory (tests only).
    /// </summary>
    public string Provider { get; set; } = "PostgreSql";

    /// <summary>
    /// Applies pending EF Core migrations when the API starts.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; } = true;

    /// <summary>
    /// Seeds demo dashboard and user data when the database is empty.
    /// </summary>
    public bool SeedDemoData { get; set; } = true;
}
