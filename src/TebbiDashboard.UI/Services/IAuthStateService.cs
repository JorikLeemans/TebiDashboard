using TebbiDashboard.Shared.Constants;
using TebbiDashboard.Shared.Dtos;

namespace TebbiDashboard.UI.Services;

/// <summary>
/// Tracks whether the user is signed in and exposes session data to the UI.
/// </summary>
public interface IAuthStateService
{
    bool IsAuthenticated { get; }

    string? Token { get; }

    string? Username { get; }

    string? Role { get; }

    event Action? OnChange;

    bool CanAccessAdminDashboard();

    Task InitializeAsync();

    Task SignInAsync(LoginResponseDto response);

    Task SignOutAsync();
}
