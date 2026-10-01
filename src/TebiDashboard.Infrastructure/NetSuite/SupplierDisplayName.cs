namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Builds the stored supplier label the same way customers are labelled.
/// Records Browser: with Auto-Generated Numbering, <c>entityId</c> is the vendor code
/// and <c>companyName</c> is the company.
/// https://system.netsuite.com/help/helpcenter/en_US/srbrowser/Browser2026_1/schema/record/vendor.html
/// </summary>
internal static class SupplierDisplayName
{
    public const string Unknown = "Unknown supplier";

    /// <summary><c>entityId</c> plus <c>companyName</c>, without repeating the code.</summary>
    public static string From(string? entityId, string? companyName)
    {
        var code = TrimToNull(entityId);
        var label = TrimToNull(companyName);

        if (code is null && label is null)
        {
            return Unknown;
        }

        if (code is null)
        {
            return label!;
        }

        if (label is null || ContainsIgnoreCase(code, label))
        {
            return code;
        }

        // Some accounts already store "L12345 Company" on entityId or companyName.
        if (ContainsIgnoreCase(label, code))
        {
            return label;
        }

        return $"{code} {label}";
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ContainsIgnoreCase(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
