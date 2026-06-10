using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.Data;
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

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var provider = ConnectionStringResolver.UsesPostgres(configuration)
            ? "PostgreSql"
            : databaseOptions.Provider ?? "PostgreSql";

        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            // Fast in-memory mode for automated tests.
            services.AddScoped<INetSuiteService, MockNetSuiteService>();
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
        services.AddScoped<IUserStore, EfUserStore>();
        services.AddScoped<IDemoDataAdminService, DatabaseDemoDataAdminService>();
        services.AddHostedService<DatabaseMigrationHostedService>();

        return services;
    }
}
