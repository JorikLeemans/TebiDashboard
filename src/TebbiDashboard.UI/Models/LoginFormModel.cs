using System.ComponentModel.DataAnnotations;

namespace TebbiDashboard.UI.Models;

/// <summary>
/// Form model for the login page with client-side validation.
/// </summary>
public sealed class LoginFormModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
