namespace TebbiDashboard.Shared.Dtos;

/// <summary>
/// Request to reset a user's password from the admin dashboard.
/// </summary>
public sealed class UpdateUserPasswordRequestDto
{
    public string Password { get; init; } = string.Empty;

    public string ConfirmPassword { get; init; } = string.Empty;
}
