using Microsoft.Extensions.DependencyInjection;
using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Application.Services;

namespace TebbiDashboard.Application;

/// <summary>
/// Registers application-layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
