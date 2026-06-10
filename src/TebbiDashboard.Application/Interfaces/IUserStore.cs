using TebbiDashboard.Shared.Dtos;

namespace TebbiDashboard.Application.Interfaces;

/// <summary>
/// Demo user storage for registration, login and admin account management.
/// </summary>
public interface IUserStore
{
    bool TryRegister(string email, string password, out string? errorMessage);

    bool TryAuthenticate(string usernameOrEmail, string password, out AuthenticatedUser? user);

    IReadOnlyList<UserAccountDto> GetAllUsers();

    bool TryUpdateRole(string email, string role, out string? errorMessage);

    bool TryUpdatePassword(string email, string password, out string? errorMessage);

    bool TryDeleteUser(string email, out string? errorMessage);
}

/// <summary>
/// User identity returned after successful authentication.
/// </summary>
public sealed class AuthenticatedUser
{
    public string Email { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
