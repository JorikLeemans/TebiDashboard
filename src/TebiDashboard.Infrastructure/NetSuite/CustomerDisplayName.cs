namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Builds the stored customer label from NetSuite customer fields.
/// Records Browser: with Auto-Generated Numbering, <c>entityId</c> is the customer code
/// (K######) and the legal name lives in <c>companyName</c>. <c>isPerson</c> is Individual
/// vs Company. <c>glommedName</c> is first+middle+last for individuals (SuiteQL; not listed
/// in the Records Browser).
/// https://system.netsuite.com/help/helpcenter/en_US/srbrowser/Browser2026_1/schema/record/customer.html
/// </summary>
internal static class CustomerDisplayName
{
    public const string Unknown = "Unknown customer";

    /// <summary>
    /// <c>entityId</c> plus <c>glommedName</c> when <c>isPerson</c> is T, otherwise plus
    /// <c>companyName</c>.
    /// </summary>
    public static string From(string? entityId, string? isPerson, string? glommedName, string? companyName)
    {
        var code = TrimToNull(entityId);
        var label = IsIndividual(isPerson) ? TrimToNull(glommedName) : TrimToNull(companyName);

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

        // Some accounts already store "K12345 Company" on entityId or companyName.
        if (ContainsIgnoreCase(label, code))
        {
            return label;
        }

        return $"{code} {label}";
    }

    // SuiteQL REST returns T/F (and sometimes true/false) for checkbox fields such as isPerson.
    private static bool IsIndividual(string? isPerson)
    {
        if (string.IsNullOrWhiteSpace(isPerson))
        {
            return false;
        }

        var value = isPerson.Trim();
        return value.Equals("T", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value == "1";
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ContainsIgnoreCase(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
