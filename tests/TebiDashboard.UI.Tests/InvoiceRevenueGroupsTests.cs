using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

/// <summary>
/// Groups customer revenue lines under their parent invoice.
/// </summary>
public sealed class InvoiceRevenueGroupsTests
{
    [Fact]
    public void From_SumsAmounts_AndKeepsLinesForTheSameInvoice()
    {
        var groups = InvoiceRevenueGroups.From(
        [
            new RevenueDto
            {
                Id = "1842-1",
                InvoiceId = "1842",
                InvoiceNumber = "F-100",
                Date = new DateOnly(2026, 9, 23),
                Amount = 10.25m,
                ItemName = "Klinkers"
            },
            new RevenueDto
            {
                Id = "1842-2",
                InvoiceId = "1842",
                InvoiceNumber = "F-100",
                Date = new DateOnly(2026, 9, 23),
                Amount = 5.50m,
                ItemName = "Zand"
            },
            new RevenueDto
            {
                Id = "2001-1",
                InvoiceId = "2001",
                InvoiceNumber = "F-200",
                Date = new DateOnly(2026, 9, 20),
                Amount = 3.00m,
                ItemName = "Tegels"
            }
        ]);

        Assert.Equal(2, groups.Count);
        Assert.Equal("F-100", groups[0].InvoiceLabel);
        Assert.Equal(15.75m, groups[0].TotalAmount);
        Assert.Equal(2, groups[0].Lines.Count);
        Assert.Equal("F-200", groups[1].InvoiceLabel);
    }

    [Fact]
    public void GroupKey_UsesInvoiceId_BeforeDocumentNumber()
    {
        var key = InvoiceRevenueGroups.GroupKey(new RevenueDto
        {
            Id = "1842-1",
            InvoiceId = "1842",
            InvoiceNumber = "F-100"
        });

        Assert.Equal("id:1842", key);
    }
}
