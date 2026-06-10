using Microsoft.AspNetCore.Mvc;
using TebbiDashboard.Shared.Constants;
using TebbiDashboard.Shared.Dtos;

namespace TebbiDashboard.Api.Controllers;

/// <summary>
/// Simple demo authentication for development.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        // Demo credentials — replace with real identity provider for production.
        if (request.Username != "demo" || request.Password != "demo")
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var response = new LoginResponseDto
        {
            Token = "demo-token",
            Username = request.Username,
            Role = Roles.Management
        };

        return Ok(response);
    }
}
