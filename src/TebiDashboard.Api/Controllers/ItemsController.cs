using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Item lookup including barcode scan results.
/// Period query values select the invoice amount shown as sold in that range.
/// </summary>
[ApiController]
[Route("api/items")]
public class ItemsController(INetSuiteService netSuiteService) : ControllerBase
{
    [HttpGet("{itemId}")]
    public async Task<IActionResult> GetById(
        string itemId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var item = await netSuiteService.GetItemByIdAsync(itemId, CreateFilter(from, to), cancellationToken);

        if (item is null)
        {
            return NotFound(new { message = $"No item found with id '{itemId}'." });
        }

        return Ok(item);
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(
        string barcode,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var item = await netSuiteService.GetItemByBarcodeAsync(barcode, CreateFilter(from, to), cancellationToken);

        if (item is null)
        {
            return NotFound(new { message = $"No item found for barcode '{barcode}'." });
        }

        return Ok(item);
    }

    private static DateRangeFilter CreateFilter(DateOnly? from, DateOnly? to) => new()
    {
        From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
        To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
    };
}
