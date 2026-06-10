using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Application.Options;
using TebbiDashboard.Infrastructure.Services;

namespace TebbiDashboard.Infrastructure;

/// <summary>
/// Registers infrastructure services including NetSuite gateway.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NetSuiteOptions>(configuration.GetSection(NetSuiteOptions.SectionName));

        var useMock = configuration.GetValue<bool>($"{NetSuiteOptions.SectionName}:UseMock", true);

        if (useMock)
        {
            services.AddScoped<INetSuiteService, MockNetSuiteService>();
        }
        else
        {
            // Placeholder for live NetSuite integration (NetSuiteService).
            services.AddScoped<INetSuiteService, MockNetSuiteService>();
        }

        return services;
    }
}
