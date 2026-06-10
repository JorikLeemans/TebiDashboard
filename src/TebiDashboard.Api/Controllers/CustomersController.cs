using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Customer master data endpoints.
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
}
