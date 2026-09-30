using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Health check for monitoring and tests.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", service = "TebiDashboard.Api" });
}
