using Microsoft.AspNetCore.Mvc;

namespace TebbiDashboard.Api.Controllers;

/// <summary>
/// Health check for monitoring and tests.
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", service = "TebbiDashboard.Api" });
}
