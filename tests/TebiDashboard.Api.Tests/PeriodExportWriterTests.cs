using ClosedXML.Excel;
using TebiDashboard.Api.Exports;
using TebiDashboard.Application.Reports;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Tests;

/// <summary>
/// Excel sheets and a PDF document for one known period.
/// </summary>
public class PeriodExportWriterTests
{
    [Fact]
    public void Excel_WritesSummaryRevenueExpensesAndInventory()
    {
        var bytes = PeriodExcelWriter.Write(SampleReport());

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(["Summary", "Revenue", "Expenses", "Inventory"], workbook.Worksheets.Select(sheet => sheet.Name).ToArray());

        var summary = workbook.Worksheet("Summary");
        Assert.Equal(1250d, ValueNextTo(summary, "Revenue"));
        Assert.Equal("42,0% (Q4 2026)", TextNextTo(summary, "Goal progress"));

        var revenue = workbook.Worksheet("Revenue");
        Assert.Equal("Invoice", revenue.Cell(1, 2).GetString());
        Assert.Equal("F-2026-014", revenue.Cell(2, 2).GetString());
        Assert.Equal(4500.5d, revenue.Cell(2, 6).GetDouble());

        var expenses = workbook.Worksheet("Expenses");
        Assert.Equal("Supplier", expenses.Cell(1, 3).GetString());
        Assert.Equal("L12345 BetonCentrale Midden-NL", expenses.Cell(2, 3).GetString());
        Assert.Equal(800d, expenses.Cell(2, 5).GetDouble());

        var inventory = workbook.Worksheet("Inventory");
        Assert.Equal("Item", inventory.Cell(1, 1).GetString());
        Assert.Equal("Betonstraatstenen Antraciet", inventory.Cell(2, 1).GetString());
        Assert.Equal("Low stock", inventory.Cell(2, 6).GetString());
    }

    [Fact]
    public void ExcelFileName_UsesTheInclusivePeriod()
    {
        var name = PeriodExportFile.Name(SampleReport(), "xlsx");

        Assert.Equal("tebi-2026-09-01-2026-09-30.xlsx", name);
    }

    [Fact]
    public void Pdf_RendersADocument()
    {
        var bytes = PeriodPdfWriter.Write(SampleReport());

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF"u8.ToArray(), bytes.Take(4).ToArray());
    }

    private static double ValueNextTo(IXLWorksheet sheet, string label)
    {
        var row = sheet.Column(1).CellsUsed().First(cell => cell.GetString() == label).Address.RowNumber;
        return sheet.Cell(row, 2).GetDouble();
    }

    private static string TextNextTo(IXLWorksheet sheet, string label)
    {
        var row = sheet.Column(1).CellsUsed().First(cell => cell.GetString() == label).Address.RowNumber;
        return sheet.Cell(row, 2).GetString();
    }

    private static PeriodReport SampleReport() => new()
    {
        GoalPeriodLabel = "Q4 2026",
        GoalPercentage = 42m,
        RevenueTrend = "+12% vs 30-08-2026 – 31-08-2026",
        Overview = new PeriodOverviewDto
        {
            PeriodFrom = new DateOnly(2026, 9, 1),
            PeriodTo = new DateOnly(2026, 9, 30),
            Summary = new DashboardSummaryDto
            {
                Revenue = 1250m,
                SalesVolume = 20m,
                GoalPercentage = 10m,
                RevenueTrend = "up",
                LowStockAlertCount = 1,
                PeriodFrom = new DateOnly(2026, 9, 1),
                PeriodTo = new DateOnly(2026, 9, 30)
            },
            Revenue =
            [
                new RevenueDto
                {
                    Date = new DateOnly(2026, 9, 14),
                    InvoiceNumber = "F-2026-014",
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    ItemName = "Betonstraatstenen Antraciet",
                    Amount = 4500.5m
                }
            ],
            Expenses =
            [
                new ExpenseDto
                {
                    Date = new DateOnly(2026, 9, 10),
                    SupplierId = "s1",
                    SupplierName = "L12345 BetonCentrale Midden-NL",
                    Category = "Vendor Bill",
                    Amount = 800m
                }
            ],
            InventoryWithActivity =
            [
                new InventoryItemDto
                {
                    ItemName = "Betonstraatstenen Antraciet",
                    Barcode = "8712345000011",
                    QuantityOnHand = 12m,
                    UnitsSold = 20m,
                    RecentSalesVolume = 4500.5m,
                    IsLowStock = true
                }
            ],
            Trend =
            [
                new PeriodTrendPointDto
                {
                    From = new DateOnly(2026, 8, 2),
                    To = new DateOnly(2026, 8, 31),
                    Revenue = 1000m,
                    Expenses = 400m
                }
            ]
        }
    };
}
