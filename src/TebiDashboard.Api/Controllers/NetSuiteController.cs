using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Api.Authorization;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// NetSuite connectivity diagnostics and data sync endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NetSuiteController(
    INetSuiteConnectionService connectionService,
    INetSuiteSyncCoordinator syncCoordinator) : ControllerBase
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
    /// Starts importing NetSuite data in the background (admin only).
    /// </summary>
    [HttpPost("sync")]
    public IActionResult StartSyncDashboardData()
    {
        if (!AdminRequestContext.IsAdmin(Request))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!syncCoordinator.TryStart())
        {
            return Conflict(syncCoordinator.GetStatus().ToStatusDto());
        }

        return Accepted(syncCoordinator.GetStatus().ToStatusDto());
    }

    /// <summary>
    /// Returns the current or last background NetSuite sync status (admin only).
    /// </summary>
    [HttpGet("sync/status")]
    public IActionResult GetSyncDashboardDataStatus()
    {
        if (!AdminRequestContext.IsAdmin(Request))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(syncCoordinator.GetStatus().ToStatusDto());
    }
}
