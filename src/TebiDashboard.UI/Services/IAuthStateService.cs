using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Services;

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

    bool CanManageGoals();

    /// <summary>
    /// Signed-in roles other than ReadOnly may download the period report.
    /// A default keeps existing test doubles working when they do not override it.
    /// </summary>
    bool CanExport() => RoleAuthorization.CanExport(Role);

    Task InitializeAsync();

    Task SignInAsync(LoginResponseDto response);

    Task SignOutAsync();
}
