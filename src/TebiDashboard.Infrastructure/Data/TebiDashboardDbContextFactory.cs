using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TebiDashboard.Infrastructure.Data;

/// <summary>
/// Design-time factory so EF Core migrations can be created from the CLI.
/// </summary>
public sealed class TebiDashboardDbContextFactory : IDesignTimeDbContextFactory<TebiDashboardDbContext>
{
    public TebiDashboardDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TebiDashboard.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = ConnectionStringResolver.Resolve(configuration);
        var provider = Environment.GetEnvironmentVariable("Database__Provider")
            ?? configuration.GetSection("Database")["Provider"]
            ?? "PostgreSql";

        var optionsBuilder = new DbContextOptionsBuilder<TebiDashboardDbContext>();

        if (string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlite(connectionString);
        }

        return new TebiDashboardDbContext(optionsBuilder.Options);
    }
}
