using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Inventory and stock endpoints.
/// </summary>
[ApiController]
[Route("api/inventory")]
public class InventoryController(INetSuiteService netSuiteService) : ControllerBase
{
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var filter = new DateRangeFilter
        {
            From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var items = await netSuiteService.GetInventoryItemsAsync(filter, cancellationToken);
        return Ok(items);
    }

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
