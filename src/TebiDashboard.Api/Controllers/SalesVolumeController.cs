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
    /// <summary>
    /// Short advice list for sales. Purchasing uses the longer ranked list on <c>top</c>.
    /// </summary>
    private const int PopularLimit = 5;

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var result = await netSuiteService.GetTopSellingItemsAsync(
            CreateFilter(from, to),
            PopularLimit,
            productGroupId: null,
            cancellationToken);

        return Ok(result.Items);
    }

    [HttpGet("top")]
    public async Task<IActionResult> GetTop(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int limit = 20,
        [FromQuery] string? productGroupId = null,
        CancellationToken cancellationToken = default)
    {
        // Keep the list short enough for the purchasing page.
        var cappedLimit = Math.Clamp(limit, 1, 100);
        var result = await netSuiteService.GetTopSellingItemsAsync(
            CreateFilter(from, to),
            cappedLimit,
            productGroupId,
            cancellationToken);
        return Ok(result);
    }

    private static DateRangeFilter CreateFilter(DateOnly? from, DateOnly? to) => new()
    {
        From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
        To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
    };
}
