using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Parent sales-order text for open-order line rows.
/// </summary>
public static class SalesOrderLabel
{
    /// <summary>
    /// Drop a trailing numeric line suffix ("t1-2", "1842-10") so several lines share one order.
    /// Ids such as "t-so-12" stay whole because the parent would still contain a dash.
    /// </summary>
    public static string For(OpenTransactionDto order) => ParentIdFromLineKey(order.TransactionId);

    /// <summary>
    /// Line keys are stored as "{orderId}-{lineId}". Demo extras use the same pattern (t1-2).
    /// </summary>
    public static string ParentIdFromLineKey(string id)
    {
        var lastDash = id.LastIndexOf('-');
        if (lastDash <= 0)
        {
            return id;
        }

        var linePart = id[(lastDash + 1)..];
        if (linePart.Length == 0 || !linePart.All(char.IsDigit))
        {
            return id;
        }

        var parent = id[..lastDash];
        // "t-so-12" is a full mock order id, not a line of "t-so".
        return parent.Contains('-', StringComparison.Ordinal) ? id : parent;
    }
}
