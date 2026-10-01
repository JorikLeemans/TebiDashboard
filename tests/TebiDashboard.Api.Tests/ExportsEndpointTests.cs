using System.Net;
using System.Net.Http.Json;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Tests;

/// <summary>
/// Period export endpoint: signed-in roles can download, ReadOnly cannot.
/// </summary>
public class ExportsEndpointTests
{
    [Fact]
    public async Task PeriodExport_ReturnsUnauthorized_WithoutAToken()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/exports/period?format=xlsx");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PeriodExcel_ReturnsTheWorkbook_ForManagement()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        await TestAuth.SignInAsync(client);

        var response = await client.GetAsync("/api/exports/period?format=xlsx&from=2026-09-01&to=2026-09-30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("tebi-2026-09-01-2026-09-30.xlsx", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public async Task PeriodPdf_ReturnsADocument_ForManagement()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        await TestAuth.SignInAsync(client);

        var response = await client.GetAsync("/api/exports/period?format=pdf&from=2026-09-01&to=2026-09-30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF"u8.ToArray(), bytes.Take(4).ToArray());
    }

    [Fact]
    public async Task PeriodExport_ReturnsForbidden_ForReadOnly()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        var email = $"readonly.{Guid.NewGuid():N}@tebi.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "password123",
            ConfirmPassword = "password123"
        });
        register.EnsureSuccessStatusCode();

        await TestAuth.SignInAsync(client, "admin@tebi.nl", "admin1234");
        var roleResponse = await client.PutAsJsonAsync(
            $"/api/admin/users/{Uri.EscapeDataString(email)}/role",
            new UpdateUserRoleRequestDto { Role = Roles.ReadOnly });
        roleResponse.EnsureSuccessStatusCode();

        await TestAuth.SignInAsync(client, email, "password123");
        var response = await client.GetAsync("/api/exports/period?format=xlsx&from=2026-09-01&to=2026-09-30");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PeriodExport_ReturnsBadRequest_WhenTheRangeIsReversed()
    {
        await using var factory = new TebiWebApplicationFactory();
        using var client = factory.CreateClient();
        await TestAuth.SignInAsync(client);

        var response = await client.GetAsync("/api/exports/period?format=xlsx&from=2026-09-30&to=2026-09-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
