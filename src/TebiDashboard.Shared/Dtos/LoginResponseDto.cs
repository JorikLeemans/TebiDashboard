namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// JWT token response after successful login.
/// </summary>
public sealed class LoginResponseDto
{
    public string Token { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
