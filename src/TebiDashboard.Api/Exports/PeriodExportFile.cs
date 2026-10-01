using TebiDashboard.Application.Reports;

namespace TebiDashboard.Api.Exports;

/// <summary>
/// Download name for a period report. The dates are the inclusive filter the user had selected.
/// </summary>
public static class PeriodExportFile
{
    public static string Name(PeriodReport report, string extension) =>
        $"tebi-{report.Overview.PeriodFrom:yyyy-MM-dd}-{report.Overview.PeriodTo:yyyy-MM-dd}.{extension}";
}
