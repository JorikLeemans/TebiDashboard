using System.Net.Http.Headers;
using System.Net.Http.Json;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Services;

/// <summary>
/// Calls administrator API endpoints from Blazor UI.
/// </summary>
public sealed class AdminApiClient(HttpClient httpClient, IAuthStateService authState)
{
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

        return await response.Content.ReadFromJsonAsync<NetSuiteConnectionResultDto>(cancellationToken)
            ?? new NetSuiteConnectionResultDto { Message = "Empty response from connection test." };
    }

    /// <summary>
    /// Imports NetSuite data into the dashboard database (admin only).
    /// </summary>
    public async Task<NetSuiteSyncResultDto> SyncNetSuiteDataAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "api/netsuite/sync");
        var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<NetSuiteSyncResultDto>(cancellationToken);
        if (body is null)
        {
            return new NetSuiteSyncResultDto { Message = "Empty response from sync endpoint." };
        }

        if (!response.IsSuccessStatusCode)
        {
            return new NetSuiteSyncResultDto
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(body.Message)
                    ? response.StatusCode == System.Net.HttpStatusCode.Forbidden
                        ? "You do not have permission to sync NetSuite data."
                        : $"Sync failed (HTTP {(int)response.StatusCode})."
                    : body.Message,
                CompletedAt = body.CompletedAt
            };
        }

        return body;
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
            return (false, response.StatusCode == System.Net.HttpStatusCode.Forbidden
                ? "You do not have permission to perform this action."
                : "Request failed.");
        }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; init; }
    }
}
