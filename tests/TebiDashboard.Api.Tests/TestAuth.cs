using System.Net.Http.Headers;
using System.Net.Http.Json;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Tests;

/// <summary>
/// Signs a test client in with a real JWT from the login endpoint.
/// </summary>
internal static class TestAuth
{
    public static async Task SignInAsync(HttpClient client, string username = "demo", string password = "demo")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = username,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }
}
