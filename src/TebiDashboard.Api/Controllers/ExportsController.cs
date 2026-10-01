using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TebiDashboard.Api.Exports;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Api.Controllers;

/// <summary>
/// Downloads the selected dashboard period as Excel or PDF.
/// Read-only users are rejected here as well as hidden in the UI.
/// </summary>
[ApiController]
[Authorize(Roles = RoleAuthorization.ExportRoles)]
[Route("api/exports")]
public class ExportsController(IPeriodReportService periodReportService) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("period")]
    public async Task<IActionResult> GetPeriod(
        [FromQuery] string? format,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var extension = format?.Trim().ToLowerInvariant();
        if (extension is not ("xlsx" or "pdf"))
        {
            return BadRequest(new { message = "Format must be xlsx or pdf." });
        }

        var filter = new DateRangeFilter
        {
            From = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            To = to ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        if (filter.To < filter.From)
        {
            return BadRequest(new { message = "The end date must be on or after the start date." });
        }

        var report = await periodReportService.BuildAsync(filter, cancellationToken);
        var fileName = PeriodExportFile.Name(report, extension);
        var bytes = extension == "xlsx"
            ? PeriodExcelWriter.Write(report)
            : PeriodPdfWriter.Write(report);
        var contentType = extension == "xlsx" ? ExcelContentType : "application/pdf";
        return File(bytes, contentType, fileName);
    }
}
