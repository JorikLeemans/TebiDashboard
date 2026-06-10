using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Services;

/// <summary>
/// Result of an auth API call for login and registration flows.
/// </summary>
public sealed class AuthApiResult
{
    public bool IsSuccess { get; init; }

    public LoginResponseDto? Response { get; init; }

    public string? ErrorMessage { get; init; }

    public static AuthApiResult Success(LoginResponseDto response) => new()
    {
        IsSuccess = true,
        Response = response
    };

    public static AuthApiResult Failure(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };
}
