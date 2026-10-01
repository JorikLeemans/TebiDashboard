using TebiDashboard.Application.Reports;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Builds the period snapshot that Excel and PDF exports share.
/// </summary>
public interface IPeriodReportService
{
    Task<PeriodReport> BuildAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
}
