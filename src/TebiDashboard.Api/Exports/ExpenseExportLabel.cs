using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Exports;

/// <summary>
/// Supplier name written into exports. Same order as the expenses table:
/// the current supplier name, then the name stored on the bill, then the id.
/// </summary>
internal static class ExpenseExportLabel
{
    public static string Supplier(ExpenseDto expense)
    {
        if (!string.IsNullOrWhiteSpace(expense.SupplierName)
            && !string.Equals(expense.SupplierName, expense.SupplierId, StringComparison.OrdinalIgnoreCase))
        {
            return expense.SupplierName;
        }

        if (!string.IsNullOrWhiteSpace(expense.VendorName))
        {
            return expense.VendorName.Trim();
        }

        return expense.SupplierId ?? string.Empty;
    }
}
