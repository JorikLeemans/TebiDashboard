using System.Globalization;

namespace TebiDashboard.Shared.Formatting;

/// <summary>
/// Formats monetary amounts with the Dutch euro symbol (€) instead of the generic currency sign (¤).
/// </summary>
public static class EuroFormatting
{
    private static readonly CultureInfo DutchCulture = CultureInfo.GetCultureInfo("nl-NL");

    /// <summary>
    /// Formats a decimal amount as euros with no decimal places (e.g. € 42.000).
    /// </summary>
    public static string FormatWhole(decimal amount) => amount.ToString("C0", DutchCulture);

    /// <summary>
    /// Formats a decimal amount as euros with two decimal places (e.g. € 42.000,50).
    /// </summary>
    public static string Format(decimal amount) => amount.ToString("C2", DutchCulture);
}
