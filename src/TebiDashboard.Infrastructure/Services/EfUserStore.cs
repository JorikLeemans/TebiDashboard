using Microsoft.EntityFrameworkCore;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.Data.Entities;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// EF Core backed user store for persistent accounts.
/// </summary>
public sealed class EfUserStore(TebiDashboardDbContext dbContext) : IUserStore
{
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

        if (dbContext.Users.Any(u => u.Email == normalizedEmail))
        {
            errorMessage = "An account with this email already exists.";
            return false;
        }

        dbContext.Users.Add(new UserEntity
        {
            Email = normalizedEmail,
            Password = password,
            Role = Roles.Sales
        });
        dbContext.SaveChanges();
        return true;
    }

    public bool TryAuthenticate(string usernameOrEmail, string password, out AuthenticatedUser? user)
    {
        user = null;
        var key = NormalizeEmail(usernameOrEmail);
        var stored = dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Email == key);

        if (stored is null || stored.Password != password)
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
        dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
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

        var existing = dbContext.Users.FirstOrDefault(u => u.Email == normalizedEmail);
        if (existing is null)
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

        existing.Role = role;
        dbContext.SaveChanges();
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

        var existing = dbContext.Users.FirstOrDefault(u => u.Email == normalizedEmail);
        if (existing is null)
        {
            errorMessage = "User not found.";
            return false;
        }

        existing.Password = password;
        dbContext.SaveChanges();
        return true;
    }

    public bool TryDeleteUser(string email, out string? errorMessage)
    {
        errorMessage = null;
        var normalizedEmail = NormalizeEmail(email);
        var existing = dbContext.Users.FirstOrDefault(u => u.Email == normalizedEmail);

        if (existing is null)
        {
            errorMessage = "User not found.";
            return false;
        }

        if (string.Equals(existing.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
            && CountAdmins() <= 1)
        {
            errorMessage = "Cannot delete the last administrator.";
            return false;
        }

        dbContext.Users.Remove(existing);
        dbContext.SaveChanges();
        return true;
    }

    private int CountAdmins() =>
        dbContext.Users.Count(u => u.Role == Roles.Admin);

    private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();
}
