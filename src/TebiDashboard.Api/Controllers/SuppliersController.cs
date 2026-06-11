using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Supplier master data and overview endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SuppliersController(INetSuiteService netSuiteService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var suppliers = await netSuiteService.GetSuppliersAsync(cancellationToken);
        return Ok(suppliers);
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var filter = BuildDateFilter(from, to);
        var overview = await netSuiteService.GetSuppliersOverviewAsync(filter, cancellationToken);
        return Ok(overview);
    }

    // Exclude the overview route segment so it is not treated as a supplier id.
    [HttpGet("{supplierId:regex(^(?!overview$).+)}")]
    public async Task<IActionResult> GetById(
        string supplierId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var filter = BuildDateFilter(from, to);
        var detail = await netSuiteService.GetSupplierDetailAsync(supplierId, filter, cancellationToken);

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
