using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Goal tracking and target configuration endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GoalsController(
    IDashboardService dashboardService,
    IGoalManagementService goalManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int year, [FromQuery] string? period, CancellationToken cancellationToken)
    {
        var goals = await dashboardService.GetGoalsWithProgressAsync(year, period, cancellationToken);
        return Ok(goals);
    }

    /// <summary>
    /// Creates or updates goal targets for a calendar year.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Save([FromBody] SaveGoalsRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var goals = await goalManagementService.SaveGoalsAsync(request, cancellationToken);
            return Ok(goals);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
