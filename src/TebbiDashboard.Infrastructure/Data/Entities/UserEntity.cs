namespace TebbiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Registered application user stored in the database.
/// </summary>
public sealed class UserEntity
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
