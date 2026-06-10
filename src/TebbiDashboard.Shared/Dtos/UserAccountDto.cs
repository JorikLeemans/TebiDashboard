namespace TebbiDashboard.Shared.Dtos;

/// <summary>
/// User account exposed on the administrator dashboard (no password).
/// </summary>
public sealed class UserAccountDto
{
    public string Email { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
