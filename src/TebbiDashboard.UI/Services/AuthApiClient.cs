using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TebbiDashboard.Shared.Dtos;

namespace TebbiDashboard.UI.Services;

/// <summary>
/// Calls the API auth endpoints from Blazor UI.
/// </summary>
public sealed class AuthApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AuthApiResult> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            return login is null
                ? AuthApiResult.Failure("Unexpected response from server.")
                : AuthApiResult.Success(login);
        }

        return AuthApiResult.Failure(await ReadErrorMessageAsync(response, cancellationToken)
            ?? "Invalid username or password. Please try again.");
    }

    public async Task<AuthApiResult> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/register", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            return login is null
                ? AuthApiResult.Failure("Unexpected response from server.")
                : AuthApiResult.Success(login);
        }

        return AuthApiResult.Failure(await ReadErrorMessageAsync(response, cancellationToken)
            ?? "Registration failed. Please try again.");
    }

    private static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken);
            return payload?.Message;
        }
        catch (JsonException)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.Conflict => "An account with this email already exists.",
                HttpStatusCode.BadRequest => "Please check your details and try again.",
                _ => null
            };
        }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; init; }
    }
}
