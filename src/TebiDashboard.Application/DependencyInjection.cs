using Microsoft.Extensions.DependencyInjection;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Services;

namespace TebiDashboard.Application;

/// <summary>
/// Registers application-layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IGoalManagementService, GoalManagementService>();
        return services;
    }
}
