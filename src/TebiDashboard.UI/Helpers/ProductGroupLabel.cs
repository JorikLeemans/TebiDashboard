using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Product group text shown in revenue tables.
/// </summary>
public static class ProductGroupLabel
{
    /// <summary>
    /// Prefer the artikelgroep name. Fall back to the NetSuite id when the name was not stored,
    /// so a numeric subsidiary id is still visible rather than a blank cell.
    /// </summary>
    public static string For(RevenueDto revenue) =>
        string.IsNullOrWhiteSpace(revenue.ProductGroupName)
            ? revenue.ProductGroupId ?? string.Empty
            : revenue.ProductGroupName;
}
