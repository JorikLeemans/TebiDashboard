using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Api.Authorization;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Login and registration. Both actions are anonymous; every other API route requires a JWT.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController(
    IUserStore userStore,
    IHostEnvironment environment,
    IConfiguration configuration,
    JwtAccessTokenFactory tokens) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        // The demo/demo account is for local portfolio demos. It stays off unless Development
        // or Auth:AllowDemoLogin is explicitly enabled.
        var allowDemoLogin = environment.IsDevelopment()
            || configuration.GetValue("Auth:AllowDemoLogin", false);

        if (allowDemoLogin
            && string.Equals(request.Username, "demo", StringComparison.Ordinal)
            && string.Equals(request.Password, "demo", StringComparison.Ordinal))
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

    private LoginResponseDto CreateLoginResponse(string username, string role) => new()
    {
        Token = tokens.Create(username, role),
        Username = username,
        Role = role
    };
}
