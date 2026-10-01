using System.Globalization;
using ClosedXML.Excel;
using TebiDashboard.Application.Reports;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Exports;

/// <summary>
/// Workbook for one dashboard period. Amounts stay numbers so they can be summed in Excel.
/// The euro sign is part of the number format; the PDF uses the same Dutch currency text as the pages.
/// </summary>
public static class PeriodExcelWriter
{
    private const string EuroFormat = "\"€\" #,##0.00";
    private const string WholeEuroFormat = "\"€\" #,##0";
    private const string DateFormat = "yyyy-mm-dd";

    public static byte[] Write(PeriodReport report)
    {
        using var workbook = new XLWorkbook();
        WriteSummary(workbook, report);
        WriteRevenue(workbook, report.Overview.Revenue);
        WriteExpenses(workbook, report.Overview.Expenses);
        WriteInventory(workbook, report.Overview.InventoryWithActivity);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSummary(XLWorkbook workbook, PeriodReport report)
    {
        var sheet = workbook.Worksheets.Add("Summary");
        var overview = report.Overview;
        var summary = overview.Summary;

        WriteLabel(sheet, 1, "Period from", overview.PeriodFrom);
        WriteLabel(sheet, 2, "Period to", overview.PeriodTo);
        WriteLabel(sheet, 3, "Revenue", summary.Revenue, WholeEuroFormat);
        WriteLabel(sheet, 4, "Sales volume", summary.SalesVolume, "#,##0");
        WriteLabel(sheet, 5, "Goal progress", GoalText(report));
        WriteLabel(sheet, 6, "Revenue trend", report.RevenueTrend);
        WriteLabel(sheet, 7, "Low stock alerts", summary.LowStockAlertCount);

        sheet.Cell(9, 1).Value = "Trend";
        sheet.Cell(9, 1).Style.Font.Bold = true;
        WriteHeader(sheet, 10, "From", "To", "Revenue", "Expenses");

        var row = 11;
        foreach (var point in overview.Trend)
        {
            WriteDate(sheet.Cell(row, 1), point.From);
            WriteDate(sheet.Cell(row, 2), point.To);
            WriteMoney(sheet.Cell(row, 3), point.Revenue, WholeEuroFormat);
            WriteMoney(sheet.Cell(row, 4), point.Expenses, WholeEuroFormat);
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteRevenue(XLWorkbook workbook, IReadOnlyList<RevenueDto> rows)
    {
        var sheet = workbook.Worksheets.Add("Revenue");
        WriteHeader(sheet, 1, "Date", "Invoice", "Customer id", "Customer", "Item", "Amount");

        var ordered = rows
            .OrderByDescending(row => row.Date)
            .ThenByDescending(row => row.Amount);
        var line = 2;
        foreach (var row in ordered)
        {
            WriteDate(sheet.Cell(line, 1), row.Date);
            sheet.Cell(line, 2).Value = string.IsNullOrWhiteSpace(row.InvoiceNumber) ? row.InvoiceId : row.InvoiceNumber;
            sheet.Cell(line, 3).Value = row.CustomerId;
            sheet.Cell(line, 4).Value = row.CustomerName;
            sheet.Cell(line, 5).Value = row.ItemName;
            WriteMoney(sheet.Cell(line, 6), row.Amount, EuroFormat);
            line++;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }

    private static void WriteExpenses(XLWorkbook workbook, IReadOnlyList<ExpenseDto> rows)
    {
        var sheet = workbook.Worksheets.Add("Expenses");
        WriteHeader(sheet, 1, "Date", "Supplier id", "Supplier", "Category", "Amount");

        var ordered = rows
            .OrderByDescending(row => row.Date)
            .ThenByDescending(row => row.Amount);
        var line = 2;
        foreach (var row in ordered)
        {
            WriteDate(sheet.Cell(line, 1), row.Date);
            sheet.Cell(line, 2).Value = row.SupplierId ?? string.Empty;
            sheet.Cell(line, 3).Value = ExpenseExportLabel.Supplier(row);
            sheet.Cell(line, 4).Value = row.Category;
            WriteMoney(sheet.Cell(line, 5), row.Amount, EuroFormat);
            line++;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }

    private static void WriteInventory(XLWorkbook workbook, IReadOnlyList<InventoryItemDto> rows)
    {
        var sheet = workbook.Worksheets.Add("Inventory");
        WriteHeader(sheet, 1, "Item", "Barcode", "On hand", "Units sold", "Sales amount", "Stock");

        var ordered = rows.OrderBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase);
        var line = 2;
        foreach (var row in ordered)
        {
            sheet.Cell(line, 1).Value = row.ItemName;
            sheet.Cell(line, 2).Value = row.Barcode;
            sheet.Cell(line, 3).Value = (double)row.QuantityOnHand;
            sheet.Cell(line, 3).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(line, 4).Value = (double)row.UnitsSold;
            sheet.Cell(line, 4).Style.NumberFormat.Format = "#,##0";
            WriteMoney(sheet.Cell(line, 5), row.RecentSalesVolume, EuroFormat);
            sheet.Cell(line, 6).Value = row.IsLowStock ? "Low stock" : "OK";
            line++;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }

    private static void WriteHeader(IXLWorksheet sheet, int row, params string[] labels)
    {
        for (var column = 0; column < labels.Length; column++)
        {
            var cell = sheet.Cell(row, column + 1);
            cell.Value = labels[column];
            cell.Style.Font.Bold = true;
        }
    }

    private static void WriteLabel(IXLWorksheet sheet, int row, string label, DateOnly value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        WriteDate(sheet.Cell(row, 2), value);
    }

    private static void WriteLabel(IXLWorksheet sheet, int row, string label, decimal value, string format)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        WriteMoney(sheet.Cell(row, 2), value, format);
    }

    private static void WriteLabel(IXLWorksheet sheet, int row, string label, string value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = value;
    }

    private static void WriteLabel(IXLWorksheet sheet, int row, string label, int value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = value;
    }

    private static void WriteDate(IXLCell cell, DateOnly value)
    {
        cell.Value = value.ToDateTime(TimeOnly.MinValue);
        cell.Style.DateFormat.Format = DateFormat;
    }

    private static void WriteMoney(IXLCell cell, decimal amount, string format)
    {
        cell.Value = (double)amount;
        cell.Style.NumberFormat.Format = format;
    }

    private static string GoalText(PeriodReport report)
    {
        var percentage = report.GoalPercentage.ToString("0.0", CultureInfo.GetCultureInfo("nl-NL"));
        return string.IsNullOrWhiteSpace(report.GoalPeriodLabel)
            ? $"{percentage}%"
            : $"{percentage}% ({report.GoalPeriodLabel})";
    }
}
