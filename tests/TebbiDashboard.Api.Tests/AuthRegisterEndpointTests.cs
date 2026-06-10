using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TebbiDashboard.Shared.Dtos;

namespace TebbiDashboard.Api.Tests;

public class AuthRegisterEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthRegisterEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsOk_AndAllowsLogin_WithNewAccount()
    {
        var email = $"user.{Guid.NewGuid():N}@tebi.test";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "securePass1",
            ConfirmPassword = "securePass1"
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = email,
            Password = "securePass1"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordsDoNotMatch()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = "mismatch@tebi.test",
            Password = "securePass1",
            ConfirmPassword = "otherPass1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyRegistered()
    {
        var email = $"duplicate.{Guid.NewGuid():N}@tebi.test";
        var payload = new RegisterRequestDto
        {
            Email = email,
            Password = "securePass1",
            ConfirmPassword = "securePass1"
        };

        var first = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
