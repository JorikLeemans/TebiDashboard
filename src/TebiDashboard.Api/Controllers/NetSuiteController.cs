using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Api.Authorization;
using TebiDashboard.Application.Interfaces;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// NetSuite connectivity diagnostics and data sync endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NetSuiteController(
    INetSuiteConnectionService connectionService,
    INetSuiteSyncService syncService) : ControllerBase
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

    /// <summary>
    /// Imports NetSuite master and transactional data into the dashboard database.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncDashboardData(CancellationToken cancellationToken)
    {
        if (!AdminRequestContext.IsAdmin(Request))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var result = await syncService.SyncDashboardDataAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
