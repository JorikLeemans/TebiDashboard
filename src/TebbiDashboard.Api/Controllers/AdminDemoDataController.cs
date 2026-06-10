using Microsoft.AspNetCore.Mvc;
using TebbiDashboard.Api.Authorization;
using TebbiDashboard.Application.Interfaces;

namespace TebbiDashboard.Api.Controllers;

/// <summary>
/// Administrator endpoints for reloading demo dashboard datasets.
/// </summary>
[ApiController]
[Route("api/admin/demo-data")]
public class AdminDemoDataController(IDemoDataAdminService demoDataAdminService) : ControllerBase
{
    [HttpPost("reseed")]
    public async Task<IActionResult> Reseed(CancellationToken cancellationToken)
    {
        var denied = RequireAdmin();
        if (denied is not null)
        {
            return denied;
        }

        if (!demoDataAdminService.IsAvailable)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Demo data reseed requires a persistent database." });
        }

        await demoDataAdminService.ReseedAsync(cancellationToken);
        return Ok(new { message = "Demo dashboard data reloaded." });
    }

    private IActionResult? RequireAdmin() =>
        AdminRequestContext.IsAdmin(Request) ? null : StatusCode(StatusCodes.Status403Forbidden);
}
