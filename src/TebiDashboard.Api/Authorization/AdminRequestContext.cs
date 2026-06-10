using TebiDashboard.Shared.Constants;

namespace TebiDashboard.Api.Authorization;

/// <summary>
/// Reads the signed-in user from request headers sent by the Blazor client.
/// </summary>
public static class AdminRequestContext
{
    public const string UsernameHeader = "X-Tebi-Username";
    public const string RoleHeader = "X-Tebi-Role";

    public static bool TryGetCallerRole(HttpRequest request, out string? role)
    {
        role = request.Headers[RoleHeader].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(role);
    }

    public static bool IsAdmin(HttpRequest request) =>
        TryGetCallerRole(request, out var role) && RoleAuthorization.CanAccessAdminDashboard(role);
}
