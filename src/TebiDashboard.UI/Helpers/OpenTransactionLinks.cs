using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Links an open sales or purchase order to the customer or supplier page that shows it.
/// </summary>
public static class OpenTransactionLinks
{
    public const string OrderQueryName = "order";

    /// <summary>
    /// Customer page for a sales order, supplier page for a purchase order, otherwise none
    /// (transfer orders have no party in this dashboard).
    /// </summary>
    public static string? ForParty(OpenTransactionDto line, string orderKey)
    {
        if (!string.IsNullOrWhiteSpace(line.CustomerId))
        {
            return $"/customers/{Uri.EscapeDataString(line.CustomerId)}?{OrderQueryName}={Uri.EscapeDataString(orderKey)}";
        }

        if (!string.IsNullOrWhiteSpace(line.SupplierId))
        {
            return $"/suppliers/{Uri.EscapeDataString(line.SupplierId)}?{OrderQueryName}={Uri.EscapeDataString(orderKey)}";
        }

        return null;
    }

    /// <summary>Reads ?order= from a dashboard URI so the matching order can be expanded.</summary>
    public static string? OrderKeyFromUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
        {
            return null;
        }

        var query = absolute.Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var name = Uri.UnescapeDataString(part[..separator]);
            if (string.Equals(name, OrderQueryName, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(part[(separator + 1)..]);
            }
        }

        return null;
    }
}
