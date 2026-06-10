namespace TebbiDashboard.Shared.Dtos;

/// <summary>
/// Login credentials sent to the auth endpoint.
/// </summary>
public sealed class LoginRequestDto
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
