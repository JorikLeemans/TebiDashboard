namespace TebbiDashboard.Application.Interfaces;

/// <summary>
/// Demo user storage for registration and login (replace with a database in production).
/// </summary>
public interface IUserStore
{
    bool TryRegister(string email, string password, out string? errorMessage);

    bool TryAuthenticate(string usernameOrEmail, string password, out AuthenticatedUser? user);
}

/// <summary>
/// User identity returned after successful authentication.
/// </summary>
public sealed class AuthenticatedUser
{
    public string Email { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
