namespace TebiDashboard.UI.Helpers;

/// <summary>
/// Compact NetSuite order-status letters with the full name used as a hover tooltip.
/// SuiteQL stores codes such as B, SalesOrd:B, or PurchOrd:B.
/// </summary>
public static class TransactionStatusLabel
{
    public static string Display(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        // Keep NetSuite codes and known full names as a single letter in the table cell.
        if (TryGetLetter(status, out var letter) || TryGetLetterFromName(status, out letter))
        {
            return letter.ToString();
        }

        return status.Trim();
    }

    public static string Describe(string? status, string? transactionType = null)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        if (!TryGetLetter(status, out var letter))
        {
            return status.Trim();
        }

        return InferFamily(status, transactionType) switch
        {
            StatusFamily.Sales => SalesName(letter),
            StatusFamily.Purchase => PurchaseName(letter),
            StatusFamily.Transfer => TransferName(letter),
            _ => AmbiguousName(letter)
        };
    }

    private static bool TryGetLetter(string status, out char letter)
    {
        var trimmed = status.Trim();
        var colon = trimmed.LastIndexOf(':');
        var token = colon >= 0 ? trimmed[(colon + 1)..] : trimmed;
        if (token.Length == 1 && char.IsLetter(token[0]))
        {
            letter = char.ToUpperInvariant(token[0]);
            return true;
        }

        letter = default;
        return false;
    }

    private static bool TryGetLetterFromName(string status, out char letter)
    {
        var name = status.Trim();
        // Longer phrases first so "Pending Billing/Partially Fulfilled" is not reduced to F.
        (string Phrase, char Letter)[] map =
        [
            ("Pending Billing/Partially Fulfilled", 'E'),
            ("Pending Billing/Partially Received", 'E'),
            ("Pending Receipt/Partially Fulfilled", 'E'),
            ("Pending Supervisor Approval", 'A'),
            ("Rejected by Supervisor", 'C'),
            ("Partially Fulfilled", 'D'),
            ("Partially Received", 'D'),
            ("Pending Fulfillment", 'B'),
            ("Pending Approval", 'A'),
            ("Pending Receipt", 'B'),
            ("Pending Billing", 'F'),
            ("Pending Bill", 'F'),
            ("Fully Billed", 'G'),
            ("Cancelled", 'C'),
            ("Rejected", 'C'),
            ("Received", 'G'),
            ("Closed", 'H'),
            ("Billed", 'G')
        ];

        foreach (var (phrase, mapped) in map)
        {
            if (name.Equals(phrase, StringComparison.OrdinalIgnoreCase))
            {
                letter = mapped;
                return true;
            }
        }

        letter = default;
        return false;
    }

    private static StatusFamily InferFamily(string status, string? transactionType)
    {
        var combined = $"{status} {transactionType}";
        if (combined.Contains("SalesOrd", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Sales Order", StringComparison.OrdinalIgnoreCase))
        {
            return StatusFamily.Sales;
        }

        if (combined.Contains("PurchOrd", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Purchase Order", StringComparison.OrdinalIgnoreCase))
        {
            return StatusFamily.Purchase;
        }

        if (combined.Contains("TrnfrOrd", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Transfer Order", StringComparison.OrdinalIgnoreCase))
        {
            return StatusFamily.Transfer;
        }

        return StatusFamily.Unknown;
    }

    private static string SalesName(char letter) => letter switch
    {
        'A' => "Pending Approval",
        'B' => "Pending Fulfillment",
        'C' => "Cancelled",
        'D' => "Partially Fulfilled",
        'E' => "Pending Billing/Partially Fulfilled",
        'F' => "Pending Billing",
        'G' => "Billed",
        'H' => "Closed",
        _ => letter.ToString()
    };

    private static string PurchaseName(char letter) => letter switch
    {
        'A' => "Pending Supervisor Approval",
        'B' => "Pending Receipt",
        'C' => "Rejected by Supervisor",
        'D' => "Partially Received",
        'E' => "Pending Billing/Partially Received",
        'F' => "Pending Bill",
        'G' => "Fully Billed",
        'H' => "Closed",
        _ => letter.ToString()
    };

    private static string TransferName(char letter) => letter switch
    {
        'A' => "Pending Approval",
        'B' => "Pending Fulfillment",
        'C' => "Rejected",
        'D' => "Partially Fulfilled",
        'E' => "Pending Receipt/Partially Fulfilled",
        'F' => "Pending Receipt",
        'G' => "Received",
        'H' => "Closed",
        _ => letter.ToString()
    };

    private static string AmbiguousName(char letter) => letter switch
    {
        'A' => "Pending Approval",
        'B' => "Pending Fulfillment (sales) or Pending Receipt (purchase)",
        'C' => "Cancelled (sales) or Rejected (purchase)",
        'D' => "Partially Fulfilled (sales) or Partially Received (purchase)",
        'E' => "Pending Billing / partially fulfilled or received",
        'F' => "Pending Billing (sales) or Pending Bill (purchase)",
        'G' => "Billed (sales) or Fully Billed (purchase)",
        'H' => "Closed",
        _ => letter.ToString()
    };

    private enum StatusFamily
    {
        Unknown,
        Sales,
        Purchase,
        Transfer
    }
}
