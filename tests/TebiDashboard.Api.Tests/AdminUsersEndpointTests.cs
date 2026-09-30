using System.Net;
using System.Net.Http.Json;
using TebiDashboard.Shared.Constants;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Tests;

public class AdminUsersEndpointTests : IClassFixture<TebiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminUsersEndpointTests(TebiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsForbidden_WithoutAdminRole()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users");
        await TestAuth.SignInAsync(_client);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_ForAdminRole()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users");
        await TestAuth.SignInAsync(_client, "admin@tebi.nl", "admin1234");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserAccountDto>>();
        Assert.NotNull(users);
        Assert.Contains(users!, u => u.Email == "admin@tebi.nl");
    }

    [Fact]
    public async Task AdminCanUpdateRole_AndDeleteRegisteredUser()
    {
        var email = $"staff.{Guid.NewGuid():N}@tebi.test";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "password123",
            ConfirmPassword = "password123"
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        using var roleRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/users/{Uri.EscapeDataString(email)}/role");
        await TestAuth.SignInAsync(_client, "admin@tebi.nl", "admin1234");
        roleRequest.Content = JsonContent.Create(new UpdateUserRoleRequestDto { Role = Roles.Finance });

        var roleResponse = await _client.SendAsync(roleRequest);
        Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{Uri.EscapeDataString(email)}");

        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
