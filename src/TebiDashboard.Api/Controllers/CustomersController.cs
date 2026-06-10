using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Customer master data and overview endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController(INetSuiteService netSuiteService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var customers = await netSuiteService.GetCustomersAsync(cancellationToken);
        return Ok(customers);
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var filter = BuildDateFilter(from, to);
        var overview = await netSuiteService.GetCustomersOverviewAsync(filter, cancellationToken);
        return Ok(overview);
    }

    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetById(
        string customerId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var filter = BuildDateFilter(from, to);
        var detail = await netSuiteService.GetCustomerDetailAsync(customerId, filter, cancellationToken);

        if (detail is null)
        {
            return NotFound();
        }

        return Ok(detail);
    }

    private static DateRangeFilter BuildDateFilter(DateOnly? from, DateOnly? to) => new()
    {
        From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
        To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
    };
}
