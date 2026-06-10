using Microsoft.AspNetCore.Mvc;
using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Shared.Filters;

namespace TebbiDashboard.Api.Controllers;

/// <summary>
/// Dashboard KPI endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var filter = new DateRangeFilter
        {
            From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var summary = await dashboardService.GetSummaryAsync(filter, cancellationToken);
        return Ok(summary);
    }
}
