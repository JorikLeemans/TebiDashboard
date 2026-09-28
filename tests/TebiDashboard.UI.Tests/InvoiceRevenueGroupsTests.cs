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
    public void GroupKey_UsesDisplayedInvoiceLabel()
    {
        var key = InvoiceRevenueGroups.GroupKey(new RevenueDto
        {
            Id = "1842-1",
            InvoiceId = "1842",
            InvoiceNumber = "F-100"
        });

        Assert.Equal("F-100", key);
    }

    // Production rows synced before InvoiceId existed use "{invoice}-{line}" as the only key.
    [Fact]
    public void From_GroupsOlderLineKeys_WhenInvoiceFieldsAreEmpty()
    {
        var groups = InvoiceRevenueGroups.From(
        [
            new RevenueDto
            {
                Id = "2136821-10",
                Date = new DateOnly(2026, 2, 12),
                Amount = -324.00m
            },
            new RevenueDto
            {
                Id = "2136821-11",
                Date = new DateOnly(2026, 2, 12),
                Amount = -45.76m
            },
            new RevenueDto
            {
                Id = "2136821-12",
                Date = new DateOnly(2026, 2, 12),
                Amount = -984.94m
            }
        ]);

        Assert.Single(groups);
        Assert.Equal("2136821", groups[0].InvoiceLabel);
        Assert.Equal(3, groups[0].Lines.Count);
        Assert.Equal(-1354.70m, groups[0].TotalAmount);
    }
}
