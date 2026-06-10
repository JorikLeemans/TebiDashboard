using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Demo authentication and registration for development.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserStore userStore) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        // Built-in demo account for portfolio demos.
        if (request.Username == "demo" && request.Password == "demo")
        {
            return Ok(CreateLoginResponse("demo", Roles.Management));
        }

        if (!userStore.TryAuthenticate(request.Username, request.Password, out var user) || user is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        return Ok(CreateLoginResponse(user.Email, user.Role));
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequestDto request)
    {
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Passwords do not match." });
        }

        if (!userStore.TryRegister(request.Email, request.Password, out var error))
        {
            var statusCode = error?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, new { message = error });
        }

        if (!userStore.TryAuthenticate(request.Email, request.Password, out var user) || user is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Registration failed." });
        }

        return Ok(CreateLoginResponse(user.Email, user.Role));
    }

    private static LoginResponseDto CreateLoginResponse(string username, string role) => new()
    {
        Token = $"demo-token-{Guid.NewGuid():N}",
        Username = username,
        Role = role
    };
}
