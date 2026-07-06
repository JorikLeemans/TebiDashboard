using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.NetSuite;
using TebiDashboard.Infrastructure.Services;

namespace TebiDashboard.Infrastructure;

/// <summary>
/// Registers infrastructure services including NetSuite gateway.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NetSuiteOptions>(configuration.GetSection(NetSuiteOptions.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddMemoryCache();
        services.AddHttpClient(NetSuiteHttpClientNames.Api)
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var timeoutMinutes = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<NetSuiteOptions>>()
                    .Value
                    .SyncHttpTimeoutMinutes;
                client.Timeout = TimeSpan.FromMinutes(Math.Clamp(timeoutMinutes, 1, 120));
            });
        services.AddSingleton<INetSuiteTokenProvider, NetSuiteOAuth2TokenProvider>();
        services.AddScoped<INetSuiteConnectionService, NetSuiteConnectionService>();
        services.AddScoped<INetSuiteRestClient, NetSuiteRestClient>();
        services.AddSingleton<INetSuiteSyncCoordinator, NetSuiteSyncCoordinator>();

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var provider = ConnectionStringResolver.UsesPostgres(configuration)
            ? "PostgreSql"
            : databaseOptions.Provider ?? "PostgreSql";

        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            // Fast in-memory mode for automated tests.
            services.AddScoped<INetSuiteService, MockNetSuiteService>();
            services.AddScoped<INetSuiteSyncService, NullNetSuiteSyncService>();
            services.AddScoped<IGoalStore, InMemoryGoalStore>();
            services.AddSingleton<IUserStore, InMemoryUserStore>();
            services.AddSingleton<IDemoDataAdminService, NullDemoDataAdminService>();
            return services;
        }

        var connectionString = ConnectionStringResolver.Resolve(configuration);

        services.AddDbContext<TebiDashboardDbContext>(options =>
        {
            // PostgreSQL is used locally and on Railway; SQLite remains available for legacy setups.
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

            if (string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<INetSuiteService, DatabaseNetSuiteService>();
        services.AddScoped<INetSuiteSyncService, NetSuiteSyncService>();
        services.AddScoped<IGoalStore, EfGoalStore>();
        services.AddScoped<IUserStore, EfUserStore>();
        services.AddScoped<IDemoDataAdminService, DatabaseDemoDataAdminService>();
        services.AddHostedService<DatabaseMigrationHostedService>();

        return services;
    }
}
