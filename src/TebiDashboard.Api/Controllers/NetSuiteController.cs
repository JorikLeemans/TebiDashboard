using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Application.Interfaces;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// NetSuite connectivity diagnostics for OAuth 2.0 setup.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NetSuiteController(INetSuiteConnectionService connectionService) : ControllerBase
{
    /// <summary>
    /// Verifies OAuth 2.0 credentials by requesting a token and calling the REST metadata catalog.
    /// </summary>
    [HttpGet("connection-test")]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var result = await connectionService.TestConnectionAsync(cancellationToken);
        return Ok(result);
    }
}
