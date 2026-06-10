using Microsoft.AspNetCore.Mvc;
using TebbiDashboard.Api.Authorization;
using TebbiDashboard.Application.Interfaces;
using TebbiDashboard.Shared.Dtos;

namespace TebbiDashboard.Api.Controllers;

/// <summary>
/// Administrator endpoints for managing registered user accounts.
/// </summary>
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController(IUserStore userStore) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var denied = RequireAdmin();
        if (denied is not null)
        {
            return denied;
        }

        return Ok(userStore.GetAllUsers());
    }

    [HttpPut("{email}/role")]
    public IActionResult UpdateRole(string email, [FromBody] UpdateUserRoleRequestDto request)
    {
        var denied = RequireAdmin();
        if (denied is not null)
        {
            return denied;
        }

        if (!userStore.TryUpdateRole(email, request.Role, out var error))
        {
            return BadRequest(new { message = error });
        }

        return Ok(new UserAccountDto { Email = email.Trim().ToLowerInvariant(), Role = request.Role });
    }

    [HttpPut("{email}/password")]
    public IActionResult UpdatePassword(string email, [FromBody] UpdateUserPasswordRequestDto request)
    {
        var denied = RequireAdmin();
        if (denied is not null)
        {
            return denied;
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Passwords do not match." });
        }

        if (!userStore.TryUpdatePassword(email, request.Password, out var error))
        {
            return BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpDelete("{email}")]
    public IActionResult Delete(string email)
    {
        var denied = RequireAdmin();
        if (denied is not null)
        {
            return denied;
        }

        if (!userStore.TryDeleteUser(email, out var error))
        {
            return BadRequest(new { message = error });
        }

        return NoContent();
    }

    private IActionResult? RequireAdmin() =>
        AdminRequestContext.IsAdmin(Request) ? null : StatusCode(StatusCodes.Status403Forbidden);
}
