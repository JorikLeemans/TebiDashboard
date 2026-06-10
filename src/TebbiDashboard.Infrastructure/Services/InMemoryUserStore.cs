using System.Collections.Concurrent;
using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Shared.Constants;

namespace TebbiDashboard.Infrastructure.Services;

/// <summary>
/// Thread-safe in-memory user store for development and demo registration.
/// </summary>
public sealed class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<string, StoredUser> _users = new(StringComparer.OrdinalIgnoreCase);

    public bool TryRegister(string email, string password, out string? errorMessage)
    {
        errorMessage = null;
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            errorMessage = "Email is required.";
            return false;
        }

        if (password.Length < 8)
        {
            errorMessage = "Password must be at least 8 characters.";
            return false;
        }

        if (!_users.TryAdd(normalizedEmail, new StoredUser(normalizedEmail, password, Roles.Sales)))
        {
            errorMessage = "An account with this email already exists.";
            return false;
        }

        return true;
    }

    public bool TryAuthenticate(string usernameOrEmail, string password, out AuthenticatedUser? user)
    {
        user = null;
        var key = NormalizeEmail(usernameOrEmail);

        if (!_users.TryGetValue(key, out var stored) || stored.Password != password)
        {
            return false;
        }

        user = new AuthenticatedUser
        {
            Email = stored.Email,
            Role = stored.Role
        };

        return true;
    }

    private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

    private sealed record StoredUser(string Email, string Password, string Role);
}
