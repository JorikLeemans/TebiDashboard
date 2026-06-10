using Microsoft.AspNetCore.Mvc;
using TebbiDashboard.Application.Interfaces;

namespace TebbiDashboard.Api.Controllers;

/// <summary>
/// Goal tracking endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GoalsController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int year, [FromQuery] string? period, CancellationToken cancellationToken)
    {
        var goals = await dashboardService.GetGoalsWithProgressAsync(year, period, cancellationToken);
        return Ok(goals);
    }
}
