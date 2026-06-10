using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TebiDashboard.Api.Tests;

/// <summary>
/// Test host that keeps using in-memory stores so integration tests stay fast.
/// </summary>
public sealed class TebiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Database:SeedDemoData", "false");
    }
}
