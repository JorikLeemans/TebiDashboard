using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Expense overview for finance users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExpensesController(INetSuiteService netSuiteService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var filter = new DateRangeFilter
        {
            From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var expenses = await netSuiteService.GetExpensesAsync(filter, cancellationToken);
        return Ok(expenses);
    }
}
