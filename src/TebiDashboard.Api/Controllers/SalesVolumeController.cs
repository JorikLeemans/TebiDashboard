using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Sales rankings for the selected period.
/// </summary>
[ApiController]
[Route("api/sales-volume")]
public class SalesVolumeController(INetSuiteService netSuiteService) : ControllerBase
{
    [HttpGet("top")]
    public async Task<IActionResult> GetTop(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int limit = 20,
        [FromQuery] string? productGroupId = null,
        CancellationToken cancellationToken = default)
    {
        var filter = new DateRangeFilter
        {
            From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Keep the list short enough for the purchasing page.
        var cappedLimit = Math.Clamp(limit, 1, 100);
        var result = await netSuiteService.GetTopSellingItemsAsync(filter, cappedLimit, productGroupId, cancellationToken);
        return Ok(result);
    }
}
