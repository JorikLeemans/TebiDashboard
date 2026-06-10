using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Revenue data endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RevenueController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var filter = new DateRangeFilter
        {
            From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var revenue = await dashboardService.GetRevenueAsync(filter, cancellationToken);
        return Ok(revenue);
    }
}
