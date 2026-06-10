using TebbiDashboard.Application.Services;
using TebbiDashboard.Shared.Filters;

namespace TebbiDashboard.Application.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ReturnsData_FromMockNetSuite()
    {
        var netSuite = new TebbiDashboard.Infrastructure.Services.MockNetSuiteService();
        var service = new DashboardService(netSuite);
        var filter = new DateRangeFilter();

        var summary = await service.GetSummaryAsync(filter);

        Assert.True(summary.Revenue > 0);
        Assert.True(summary.SalesVolume > 0);
    }
}
