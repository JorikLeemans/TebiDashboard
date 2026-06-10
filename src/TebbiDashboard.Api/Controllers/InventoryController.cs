using Microsoft.AspNetCore.Mvc;
using TebbiDashboard.Application.Interfaces;

namespace TebbiDashboard.Api.Controllers;

/// <summary>
/// Inventory and stock endpoints.
/// </summary>
[ApiController]
[Route("api/inventory")]
public class InventoryController(INetSuiteService netSuiteService) : ControllerBase
{
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] bool highSales = true, CancellationToken cancellationToken = default)
    {
        var items = await netSuiteService.GetLowStockItemsAsync(highSales, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{itemId}/locations")]
    public async Task<IActionResult> GetLocations(string itemId, CancellationToken cancellationToken)
    {
        var locations = await netSuiteService.GetItemLocationsAsync(itemId, cancellationToken);
        return Ok(locations);
    }

    [HttpGet("{itemId}/open-transactions")]
    public async Task<IActionResult> GetOpenTransactions(string itemId, CancellationToken cancellationToken)
    {
        var transactions = await netSuiteService.GetOpenTransactionsAsync(itemId, cancellationToken);
        return Ok(transactions);
    }
}
