using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

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

    [HttpGet("period-overview")]
    public async Task<IActionResult> GetPeriodOverview([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var filter = new DateRangeFilter
        {
            From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var overview = await dashboardService.GetPeriodOverviewAsync(filter, cancellationToken);
        return Ok(overview);
    }
}
