namespace TebiDashboard.Shared.Constants;

/// <summary>
/// Role checks used by the UI and API for access control.
/// </summary>
public static class RoleAuthorization
{
    /// <summary>
    /// Roles that may access the administrator dashboard.
    /// </summary>
    public static readonly IReadOnlySet<string> AdminDashboardRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Roles.Admin };

    public static bool CanAccessAdminDashboard(string? role) =>
        !string.IsNullOrWhiteSpace(role) && AdminDashboardRoles.Contains(role);

    /// <summary>
    /// Roles that may configure revenue and item goal targets.
    /// </summary>
    public static readonly IReadOnlySet<string> GoalManagementRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Roles.Admin, Roles.Management };

    public static bool CanManageGoals(string? role) =>
        !string.IsNullOrWhiteSpace(role) && GoalManagementRoles.Contains(role);

    /// <summary>
    /// All assignable roles for account management.
    /// </summary>
    public static IReadOnlyList<string> AssignableRoles { get; } =
    [
        Roles.Admin,
        Roles.Management,
        Roles.Purchasing,
        Roles.Inventory,
        Roles.Sales,
        Roles.Finance,
        Roles.ReadOnly
    ];
}
