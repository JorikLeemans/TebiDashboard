using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Parent invoice text for revenue line rows.
/// </summary>
public static class InvoiceLabel
{
    /// <summary>
    /// Prefer the invoice document number (tranId). Fall back to the invoice internal id,
    /// then to a "{invoiceId}-{lineId}" row key by dropping the trailing line suffix.
    /// </summary>
    public static string For(RevenueDto revenue)
    {
        if (!string.IsNullOrWhiteSpace(revenue.InvoiceNumber))
        {
            return revenue.InvoiceNumber;
        }

        if (!string.IsNullOrWhiteSpace(revenue.InvoiceId))
        {
            return revenue.InvoiceId;
        }

        return ParentIdFromLineKey(revenue.Id);
    }

    /// <summary>
    /// Line keys are stored as "{invoiceId}-{lineId}". Older rows may only have that key.
    /// </summary>
    private static string ParentIdFromLineKey(string id)
    {
        var lastDash = id.LastIndexOf('-');
        if (lastDash <= 0)
        {
            return id;
        }

        var linePart = id[(lastDash + 1)..];
        return linePart.Length > 0 && linePart.All(char.IsDigit)
            ? id[..lastDash]
            : id;
    }
}
