using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TebiDashboard.Api.Tests;

/// <summary>
/// JWT, role claims, and the development-only demo login.
/// </summary>
public sealed class AuthSecurityTests
{
    [Fact]
    public async Task Dashboard_ReturnsUnauthorized_WithoutAToken()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_IgnoresASpoofedRoleHeader()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tebi-Role", "Admin");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GoalsSave_ReturnsForbidden_ForASalesUser()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        var email = $"sales.{Guid.NewGuid():N}@tebi.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "password123",
            confirmPassword = "password123"
        });
        register.EnsureSuccessStatusCode();
        await TestAuth.SignInAsync(client, email, "password123");

        var response = await client.PutAsJsonAsync("/api/goals", new
        {
            year = DateTime.UtcNow.Year,
            targets = new[] { new { period = "YEAR", itemId = (string?)null, targetValue = 1m } }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordIsNull()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent(
            """{"email":"null-pass@tebi.test","password":null,"confirmPassword":null}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DemoLogin_IsRejected_OutsideDevelopment()
    {
        await using var factory = new ProductionWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "demo",
            password = "demo"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class ProductionWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Database:Provider", "InMemory");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
            builder.UseSetting("Database:SeedDemoData", "false");
            builder.UseSetting("Auth:SigningKey", "production-test-signing-key-32chars!!");
            builder.UseSetting("Auth:AllowDemoLogin", "false");
        }
    }
}
