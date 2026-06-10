using System.Text.Json;
using Microsoft.JSInterop;
using TebbiDashboard.Shared.Constants;
using TebbiDashboard.Shared.Dtos;

namespace TebbiDashboard.UI.Services;

/// <summary>
/// Stores auth state in memory and persists it to sessionStorage in the browser.
/// </summary>
public sealed class AuthStateService(IJSRuntime jsRuntime) : IAuthStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool IsAuthenticated { get; private set; }

    public string? Token { get; private set; }

    public string? Username { get; private set; }

    public string? Role { get; private set; }

    public event Action? OnChange;

    public bool CanAccessAdminDashboard() => RoleAuthorization.CanAccessAdminDashboard(Role);

    public async Task InitializeAsync()
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", AuthStorageKeys.SessionKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var session = JsonSerializer.Deserialize<StoredAuthSession>(json, JsonOptions);
            if (session is null || string.IsNullOrWhiteSpace(session.Token))
            {
                return;
            }

            ApplySession(session.Token, session.Username, session.Role);
        }
        catch (InvalidOperationException)
        {
            // Static prerender — JavaScript is not available yet.
        }
    }

    public async Task SignInAsync(LoginResponseDto response)
    {
        ApplySession(response.Token, response.Username, response.Role);

        var session = new StoredAuthSession
        {
            Token = response.Token,
            Username = response.Username,
            Role = response.Role
        };

        var json = JsonSerializer.Serialize(session, JsonOptions);
        await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", AuthStorageKeys.SessionKey, json);
    }

    public async Task SignOutAsync()
    {
        Token = null;
        Username = null;
        Role = null;
        IsAuthenticated = false;
        NotifyChanged();

        try
        {
            await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", AuthStorageKeys.SessionKey);
        }
        catch (InvalidOperationException)
        {
            // Ignore when JS is unavailable.
        }
    }

    private void ApplySession(string token, string username, string role)
    {
        Token = token;
        Username = username;
        Role = role;
        IsAuthenticated = true;
        NotifyChanged();
    }

    private void NotifyChanged() => OnChange?.Invoke();

    private sealed class StoredAuthSession
    {
        public string Token { get; init; } = string.Empty;

        public string Username { get; init; } = string.Empty;

        public string Role { get; init; } = string.Empty;
    }
}
