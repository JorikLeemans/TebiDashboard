namespace TebbiDashboard.Shared.Dtos;

/// <summary>
/// Request to change a user's role from the admin dashboard.
/// </summary>
public sealed class UpdateUserRoleRequestDto
{
    public string Role { get; init; } = string.Empty;
}
