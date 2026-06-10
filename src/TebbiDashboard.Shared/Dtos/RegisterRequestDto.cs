namespace TebbiDashboard.Shared.Dtos;

/// <summary>
/// Registration payload sent to the auth endpoint.
/// </summary>
public sealed class RegisterRequestDto
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string ConfirmPassword { get; init; } = string.Empty;
}
