using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TebiDashboard.Application.Reports;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Formatting;

namespace TebiDashboard.Api.Exports;

/// <summary>
/// Period report for printing. Tables continue onto extra pages when the period has many rows.
/// QuestPDF requires a license selection before the first document is built.
/// The Community license is the free option and should be reviewed before a commercial rollout.
/// </summary>
public static class PeriodPdfWriter
{
    private static readonly CultureInfo DutchCulture = CultureInfo.GetCultureInfo("nl-NL");

    static PeriodPdfWriter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Write(PeriodReport report)
    {
        var overview = report.Overview;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(text => text.FontSize(9).FontColor(Colors.Grey.Darken4));

                page.Header().Column(column =>
                {
                    column.Item().Text("TEBI Dashboard").FontSize(16).SemiBold().FontColor(Colors.Green.Darken3);
                    column.Item().PaddingTop(2).Text(PeriodLine(overview)).FontSize(10);
                });

                page.Content().PaddingTop(12).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(content => WriteKpis(content, report));
                    column.Item().Element(content => WriteTrend(content, overview.Trend));
                    column.Item().Element(content => WriteRevenue(content, overview.Revenue));
                    column.Item().Element(content => WriteExpenses(content, overview.Expenses));
                    column.Item().Element(content => WriteInventory(content, overview.InventoryWithActivity));
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void WriteKpis(IContainer container, PeriodReport report)
    {
        var summary = report.Overview.Summary;
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            Kpi(table, "Revenue", EuroFormatting.FormatWhole(summary.Revenue));
            Kpi(table, "Sales volume", summary.SalesVolume.ToString("N0", DutchCulture));
            Kpi(table, $"Goal ({report.GoalPeriodLabel})", $"{report.GoalPercentage.ToString("0.0", DutchCulture)}%");
            Kpi(table, "Low stock", summary.LowStockAlertCount.ToString(DutchCulture));
        });
    }

    private static void Kpi(TableDescriptor table, string label, string value)
    {
        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(column =>
        {
            column.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
            column.Item().PaddingTop(2).Text(value).FontSize(12).SemiBold();
        });
    }

    private static void WriteTrend(IContainer container, IReadOnlyList<PeriodTrendPointDto> points)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Trend").FontSize(12).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                Header(table, "Period", "Revenue", "Expenses");
                foreach (var point in points)
                {
                    TextCell(table, FormatWindow(point));
                    TextCell(table, EuroFormatting.FormatWhole(point.Revenue));
                    TextCell(table, EuroFormatting.FormatWhole(point.Expenses));
                }
            });
        });
    }

    private static void WriteRevenue(IContainer container, IReadOnlyList<RevenueDto> rows)
    {
        var ordered = rows.OrderByDescending(row => row.Date).ThenByDescending(row => row.Amount).ToList();
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text($"Revenue ({ordered.Count})").FontSize(12).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(62);
                    columns.RelativeColumn();
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                });
                Header(table, "Date", "Invoice", "Customer", "Item", "Amount");
                foreach (var row in ordered)
                {
                    TextCell(table, row.Date.ToString("dd-MM-yyyy", DutchCulture));
                    TextCell(table, string.IsNullOrWhiteSpace(row.InvoiceNumber) ? row.InvoiceId : row.InvoiceNumber);
                    TextCell(table, row.CustomerName);
                    TextCell(table, row.ItemName);
                    TextCell(table, EuroFormatting.Format(row.Amount));
                }
            });
        });
    }

    private static void WriteExpenses(IContainer container, IReadOnlyList<ExpenseDto> rows)
    {
        var ordered = rows.OrderByDescending(row => row.Date).ThenByDescending(row => row.Amount).ToList();
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text($"Expenses ({ordered.Count})").FontSize(12).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(62);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                Header(table, "Date", "Supplier", "Category", "Amount");
                foreach (var row in ordered)
                {
                    TextCell(table, row.Date.ToString("dd-MM-yyyy", DutchCulture));
                    TextCell(table, ExpenseExportLabel.Supplier(row));
                    TextCell(table, row.Category);
                    TextCell(table, EuroFormatting.Format(row.Amount));
                }
            });
        });
    }

    private static void WriteInventory(IContainer container, IReadOnlyList<InventoryItemDto> rows)
    {
        var ordered = rows.OrderBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase).ToList();
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text($"Inventory ({ordered.Count})").FontSize(12).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.ConstantColumn(58);
                });
                Header(table, "Item", "On hand", "Units sold", "Sales amount", "Stock");
                foreach (var row in ordered)
                {
                    TextCell(table, row.ItemName);
                    TextCell(table, row.QuantityOnHand.ToString("N0", DutchCulture));
                    TextCell(table, row.UnitsSold.ToString("N0", DutchCulture));
                    TextCell(table, EuroFormatting.Format(row.RecentSalesVolume));
                    TextCell(table, row.IsLowStock ? "Low stock" : "OK");
                }
            });
        });
    }

    private static void Header(TableDescriptor table, params string[] labels)
    {
        foreach (var label in labels)
        {
            table.Cell().Background(Colors.Green.Darken3).Padding(4).Text(label).FontColor(Colors.White).SemiBold();
        }
    }

    private static void TextCell(TableDescriptor table, string value)
    {
        table.Cell().BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(value);
    }

    private static string PeriodLine(PeriodOverviewDto overview) =>
        $"{overview.PeriodFrom.ToString("dd-MM-yyyy", DutchCulture)} – {overview.PeriodTo.ToString("dd-MM-yyyy", DutchCulture)}";

    private static string FormatWindow(PeriodTrendPointDto point) =>
        point.From == point.To
            ? point.From.ToString("dd-MM-yyyy", DutchCulture)
            : $"{point.From.ToString("dd-MM-yyyy", DutchCulture)} – {point.To.ToString("dd-MM-yyyy", DutchCulture)}";
}
