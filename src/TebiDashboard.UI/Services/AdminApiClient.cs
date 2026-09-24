using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Services;

/// <summary>
/// Calls administrator API endpoints from Blazor UI.
/// </summary>
public sealed class AdminApiClient(HttpClient httpClient, IAuthStateService authState)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<UserAccountDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/admin/users");
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to load users (HTTP {(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<UserAccountDto>>(cancellationToken) ?? [];
    }

    public async Task<(bool Success, string? Error)> UpdateRoleAsync(
        string email,
        string role,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Put, $"api/admin/users/{Uri.EscapeDataString(email)}/role");
        request.Content = JsonContent.Create(new UpdateUserRoleRequestDto { Role = role });
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> UpdatePasswordAsync(
        string email,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Put, $"api/admin/users/{Uri.EscapeDataString(email)}/password");
        request.Content = JsonContent.Create(new UpdateUserPasswordRequestDto
        {
            Password = password,
            ConfirmPassword = confirmPassword
        });
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Delete, $"api/admin/users/{Uri.EscapeDataString(email)}");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// Loads whether NetSuite is configured and the last sync snapshot for the admin page.
    /// </summary>
    public async Task<NetSuiteSetupStatusDto> GetNetSuiteSetupAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/netsuite/setup");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Keep the admin page usable when the hosting proxy never returns this GET.
            return new NetSuiteSetupStatusDto
            {
                ConfigurationMessage = "Timed out while loading NetSuite setup status.",
                DataSourceMessage = "Accounts are still available. Use Test connection or Sync once the API responds."
            };
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new NetSuiteSetupStatusDto
                {
                    ConfigurationMessage = "You do not have permission to view NetSuite setup status.",
                    DataSourceMessage = "Administrator access is required."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var fallback = await ReadRawBodyAsync(response, cancellationToken);
                return new NetSuiteSetupStatusDto
                {
                    ConfigurationMessage = DescribeNonJsonResponse(response.StatusCode, fallback),
                    DataSourceMessage = "Could not load NetSuite setup status from the API."
                };
            }

            return await ReadJsonBodyAsync<NetSuiteSetupStatusDto>(response, cancellationToken)
                ?? new NetSuiteSetupStatusDto
                {
                    ConfigurationMessage = "Empty response from NetSuite setup endpoint.",
                    DataSourceMessage = "Could not load NetSuite setup status from the API."
                };
        }
    }

    /// <summary>
    /// Verifies NetSuite OAuth credentials from the admin dashboard.
    /// </summary>
    public async Task<NetSuiteConnectionResultDto> TestNetSuiteConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/netsuite/connection-test");
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"NetSuite connection test failed (HTTP {(int)response.StatusCode}).");
        }

        return await ReadJsonBodyAsync<NetSuiteConnectionResultDto>(response, cancellationToken)
            ?? new NetSuiteConnectionResultDto { Message = "Empty response from connection test." };
    }

    /// <summary>
    /// Starts a background NetSuite import on the API (returns quickly; poll status afterward).
    /// </summary>
    public async Task<NetSuiteSyncStatusDto> StartNetSuiteSyncAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "api/netsuite/sync");
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.Conflict)
        {
            return await ReadJsonBodyAsync<NetSuiteSyncStatusDto>(response, cancellationToken)
                ?? new NetSuiteSyncStatusDto { State = "Running", Message = "Sync started." };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new NetSuiteSyncStatusDto
            {
                State = "Failed",
                Message = "You do not have permission to sync NetSuite data."
            };
        }

        var fallback = await ReadRawBodyAsync(response, cancellationToken);
        return new NetSuiteSyncStatusDto
        {
            State = "Failed",
            Message = DescribeNonJsonResponse(response.StatusCode, fallback)
        };
    }

    /// <summary>
    /// Polls background NetSuite sync progress from the API.
    /// </summary>
    public async Task<NetSuiteSyncStatusDto> GetNetSuiteSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/netsuite/sync/status");
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new NetSuiteSyncStatusDto
            {
                State = "Failed",
                Message = "You do not have permission to view NetSuite sync status."
            };
        }

        if (!response.IsSuccessStatusCode)
        {
            var fallback = await ReadRawBodyAsync(response, cancellationToken);
            return new NetSuiteSyncStatusDto
            {
                State = "Failed",
                Message = DescribeNonJsonResponse(response.StatusCode, fallback)
            };
        }

        return await ReadJsonBodyAsync<NetSuiteSyncStatusDto>(response, cancellationToken)
            ?? new NetSuiteSyncStatusDto { State = "Failed", Message = "Empty response from sync status endpoint." };
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, relativeUrl);

        if (authState.IsAuthenticated
            && !string.IsNullOrWhiteSpace(authState.Token)
            && !string.IsNullOrWhiteSpace(authState.Username)
            && !string.IsNullOrWhiteSpace(authState.Role))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Token);
            request.Headers.TryAddWithoutValidation("X-Tebi-Username", authState.Username);
            request.Headers.TryAddWithoutValidation("X-Tebi-Role", authState.Role);
        }

        return request;
    }

    private static async Task<(bool Success, string? Error)> ReadResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken);
            return (false, error?.Message ?? "Request failed.");
        }
        catch
        {
            return (false, response.StatusCode == HttpStatusCode.Forbidden
                ? "You do not have permission to perform this action."
                : "Request failed.");
        }
    }

    private static async Task<T?> ReadJsonBodyAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await ReadRawBodyAsync(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(raw, JsonOptions);
    }

    private static Task<string> ReadRawBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        response.Content.ReadAsStringAsync(cancellationToken);

    private static string DescribeNonJsonResponse(HttpStatusCode statusCode, string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return $"Sync failed (HTTP {(int)statusCode}).";
        }

        var trimmed = rawBody.Trim();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            return $"Sync failed (HTTP {(int)statusCode}).";
        }

        if (trimmed.Contains("upstream", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return "De hosting-proxy heeft de sync-afspraak afgebroken (time-out). De sync draait nu op de achtergrond op de API; vernieuw de pagina en controleer of de data binnenkomt, of probeer opnieuw na deploy van de nieuwste versie.";
        }

        var preview = trimmed.Length > 300 ? $"{trimmed[..300]}…" : trimmed;
        return $"Sync failed (HTTP {(int)statusCode}): {preview}";
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; init; }
    }
}

