using System.Collections.Concurrent;
using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Shared.Constants;
using TebbiDashboard.Shared.Dtos;

namespace TebbiDashboard.Infrastructure.Services;

/// <summary>
/// Thread-safe in-memory user store for development and demo registration.
/// </summary>
public sealed class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<string, StoredUser> _users = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryUserStore()
    {
        // Default administrator for the admin dashboard demo.
        _users["admin@tebi.nl"] = new StoredUser("admin@tebi.nl", "admin1234", Roles.Admin);
    }

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

    public IReadOnlyList<UserAccountDto> GetAllUsers() =>
        _users.Values
            .OrderBy(u => u.Email, StringComparer.OrdinalIgnoreCase)
            .Select(u => new UserAccountDto { Email = u.Email, Role = u.Role })
            .ToList();

    public bool TryUpdateRole(string email, string role, out string? errorMessage)
    {
        errorMessage = null;
        var normalizedEmail = NormalizeEmail(email);

        if (!RoleAuthorization.AssignableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            errorMessage = "Invalid role selected.";
            return false;
        }

        if (!_users.TryGetValue(normalizedEmail, out var existing))
        {
            errorMessage = "User not found.";
            return false;
        }

        if (string.Equals(existing.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
            && CountAdmins() <= 1)
        {
            errorMessage = "Cannot remove the last administrator.";
            return false;
        }

        _users[normalizedEmail] = existing with { Role = role };
        return true;
    }

    public bool TryUpdatePassword(string email, string password, out string? errorMessage)
    {
        errorMessage = null;
        var normalizedEmail = NormalizeEmail(email);

        if (password.Length < 8)
        {
            errorMessage = "Password must be at least 8 characters.";
            return false;
        }

        if (!_users.TryGetValue(normalizedEmail, out var existing))
        {
            errorMessage = "User not found.";
            return false;
        }

        _users[normalizedEmail] = existing with { Password = password };
        return true;
    }

    public bool TryDeleteUser(string email, out string? errorMessage)
    {
        errorMessage = null;
        var normalizedEmail = NormalizeEmail(email);

        if (!_users.ContainsKey(normalizedEmail))
        {
            errorMessage = "User not found.";
            return false;
        }

        if (string.Equals(_users[normalizedEmail].Role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
            && CountAdmins() <= 1)
        {
            errorMessage = "Cannot delete the last administrator.";
            return false;
        }

        return _users.TryRemove(normalizedEmail, out _);
    }

    private int CountAdmins() =>
        _users.Values.Count(u => string.Equals(u.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

    private sealed record StoredUser(string Email, string Password, string Role);
}
