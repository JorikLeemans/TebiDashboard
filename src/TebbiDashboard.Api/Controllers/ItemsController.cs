using Microsoft.AspNetCore.Mvc;
using TebbiDashboard.Application.Interfaces;

namespace TebbiDashboard.Api.Controllers;

/// <summary>
/// Item lookup including barcode scan results.
/// </summary>
[ApiController]
[Route("api/items")]
public class ItemsController(INetSuiteService netSuiteService) : ControllerBase
{
    [HttpGet("by-barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        var item = await netSuiteService.GetItemByBarcodeAsync(barcode, cancellationToken);

        if (item is null)
        {
            return NotFound(new { message = $"No item found for barcode '{barcode}'." });
        }

        return Ok(item);
    }
}
