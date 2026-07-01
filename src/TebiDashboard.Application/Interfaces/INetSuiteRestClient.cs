using System.Text.Json;

namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Low-level authenticated access to NetSuite REST and SuiteQL endpoints.
/// </summary>
public interface INetSuiteRestClient
{
    /// <summary>
    /// Executes a SuiteQL query and returns all pages of result rows.
    /// </summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, JsonElement>>> QuerySuiteQlAsync(
        string sql,
        int pageSize,
        CancellationToken cancellationToken = default);
}
