using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TebiDashboard.Api.Authorization;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.NetSuite;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// NetSuite connectivity diagnostics and data sync endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NetSuiteController(
    INetSuiteConnectionService connectionService,
    INetSuiteSyncCoordinator syncCoordinator,
    IOptions<NetSuiteOptions> netSuiteOptions) : ControllerBase
{
    /// <summary>
    /// Returns whether NetSuite credentials are present and the last sync for this API process (admin only).
    /// Used by the admin page on load so operators do not have to guess if sync can work.
    /// </summary>
    [HttpGet("setup")]
    public IActionResult GetSetupStatus()
    {
        if (!AdminRequestContext.IsAdmin(Request))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var settings = netSuiteOptions.Value;
        var lastSync = MapLastSync(syncCoordinator.GetStatus());
        var isConfigured = settings.IsConfigured;

        return Ok(new NetSuiteSetupStatusDto
        {
            IsConfigured = isConfigured,
            CanTestConnection = isConfigured,
            CanSync = isConfigured,
            MissingSettings = settings.GetMissingConfigurationKeys(),
            ConfigurationMessage = isConfigured
                ? "NetSuite OAuth credentials are configured on the API."
                : settings.DescribeMissingConfiguration(),
            DataSourceMessage = isConfigured
                ? "Dashboard pages show the last successful import in the local database. Use Sync to refresh from NetSuite."
                : "NetSuite is not configured, so dashboard pages show locally seeded demo data instead of a live import.",
            LastSync = lastSync
        });
    }

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
    /// Turns in-memory coordinator state into a DTO. Idle means no sync has run since the API started.
    /// </summary>
    private static NetSuiteSyncStatusDto MapLastSync(NetSuiteSyncRunStatus status)
    {
        var dto = status.ToStatusDto();
        if (dto.State != NetSuiteSyncStates.Idle)
        {
            return dto;
        }

        return new NetSuiteSyncStatusDto
        {
            State = NetSuiteSyncStates.Idle,
            Message = "No sync has run since this API process started.",
            StartedAt = dto.StartedAt,
            CompletedAt = dto.CompletedAt
        };
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
