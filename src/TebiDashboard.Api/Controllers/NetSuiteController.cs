using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TebiDashboard.Api.Authorization;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.NetSuite;
using TebiDashboard.Shared.Constants;
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
    INetSuiteSyncCheckpointStore checkpointStore,
    IOptions<NetSuiteOptions> netSuiteOptions) : ControllerBase
{
    /// <summary>
    /// Returns whether NetSuite credentials are present and the last sync (admin only).
    /// Last successful time comes from the database so it survives API restarts.
    /// </summary>
    [HttpGet("setup")]
    public async Task<IActionResult> GetSetupStatus(CancellationToken cancellationToken)
    {
        if (!AdminRequestContext.IsAdmin(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var settings = netSuiteOptions.Value;
        var checkpoint = await checkpointStore.GetAsync(cancellationToken);
        var lastSync = MapLastSync(syncCoordinator.GetStatus(), checkpoint);
        var isConfigured = settings.IsConfigured;
        var utcNow = DateTimeOffset.UtcNow;

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
            LastSync = lastSync,
            LookbackDaysMin = NetSuiteLookback.MinDays,
            LookbackDaysMax = NetSuiteLookback.MaxDays,
            SuggestedLookbackDays = NetSuiteLookback.Suggest(checkpoint.LastSucceededAt, utcNow),
            LastLookbackDays = checkpoint.LastLookbackDays,
            LastSucceededAt = checkpoint.LastSucceededAt
        });
    }

    /// <summary>
    /// Verifies OAuth 2.0 credentials by requesting a token and calling the REST metadata catalog.
    /// </summary>
    [HttpGet("connection-test")]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        if (!AdminRequestContext.IsAdmin(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var result = await connectionService.TestConnectionAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Prefers the in-process run when one exists; otherwise shows the persisted last success.
    /// </summary>
    private static NetSuiteSyncStatusDto MapLastSync(NetSuiteSyncRunStatus status, NetSuiteSyncCheckpoint checkpoint)
    {
        if (status.State != NetSuiteSyncStates.Idle)
        {
            return status.ToStatusDto();
        }

        if (checkpoint.LastSucceededAt is { } succeededAt)
        {
            return new NetSuiteSyncStatusDto
            {
                State = NetSuiteSyncStates.Succeeded,
                Message = string.IsNullOrWhiteSpace(checkpoint.LastMessage)
                    ? "Last successful NetSuite import."
                    : checkpoint.LastMessage,
                StartedAt = null,
                CompletedAt = succeededAt,
                LookbackDays = checkpoint.LastLookbackDays
            };
        }

        return new NetSuiteSyncStatusDto
        {
            State = NetSuiteSyncStates.Idle,
            Message = "No successful NetSuite import is stored yet.",
            StartedAt = checkpoint.LastCompletedAt,
            CompletedAt = checkpoint.LastCompletedAt,
            LookbackDays = checkpoint.LastLookbackDays
        };
    }

    /// <summary>
    /// Starts importing NetSuite data in the background (admin only).
    /// lookbackDays is the Admin slider value; SuiteQL invoices/bills use tranDate &gt;= today minus that many days.
    /// </summary>
    [HttpPost("sync")]
    public IActionResult StartSyncDashboardData([FromQuery] int lookbackDays = NetSuiteLookback.DefaultDays)
    {
        if (!AdminRequestContext.IsAdmin(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!syncCoordinator.TryStart(lookbackDays))
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
        if (!AdminRequestContext.IsAdmin(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(syncCoordinator.GetStatus().ToStatusDto());
    }
}
