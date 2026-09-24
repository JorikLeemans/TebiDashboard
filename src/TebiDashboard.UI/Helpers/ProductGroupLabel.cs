using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Product group text shown in revenue tables.
/// </summary>
public static class ProductGroupLabel
{
    /// <summary>
    /// Prefer the artikelgroep name. Fall back to a non-numeric code such as pg-edging.
    /// A bare NetSuite internal id (for example "16") is not a name — hide it instead of
    /// showing the same number in the product-group column.
    /// </summary>
    public static string For(RevenueDto revenue)
    {
        if (!string.IsNullOrWhiteSpace(revenue.ProductGroupName))
        {
            return revenue.ProductGroupName;
        }

        var id = revenue.ProductGroupId ?? string.Empty;
        return LooksLikeInternalId(id) ? string.Empty : id;
    }

    // SuiteQL subsidiary / class internal ids are digits. Demo codes use a pg- prefix.
    private static bool LooksLikeInternalId(string value) =>
        value.Length > 0 && value.All(char.IsDigit);
}
