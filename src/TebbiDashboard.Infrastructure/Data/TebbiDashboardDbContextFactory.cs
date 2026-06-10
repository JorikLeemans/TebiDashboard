using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TebbiDashboard.Infrastructure.Data;

/// <summary>
/// Design-time factory so EF Core migrations can be created from the CLI.
/// </summary>
public sealed class TebbiDashboardDbContextFactory : IDesignTimeDbContextFactory<TebbiDashboardDbContext>
{
    public TebbiDashboardDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TebbiDashboard.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tebbi.db";
        var provider = configuration.GetSection("Database")["Provider"] ?? "Sqlite";

        var optionsBuilder = new DbContextOptionsBuilder<TebbiDashboardDbContext>();

        if (string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlite(connectionString);
        }

        return new TebbiDashboardDbContext(optionsBuilder.Options);
    }
}
