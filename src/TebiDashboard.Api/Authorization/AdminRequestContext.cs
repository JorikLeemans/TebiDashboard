using System.Security.Claims;
using TebiDashboard.Shared.Constants;

namespace TebiDashboard.Api.Authorization;

/// <summary>
/// Reads the signed-in user from the validated JWT. The role is a token claim, not a request header.
/// </summary>
public static class AdminRequestContext
{
    public static bool IsAdmin(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
        && RoleAuthorization.CanAccessAdminDashboard(user.FindFirst("role")?.Value);
}
