using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

/// <summary>
/// NetSuite status letters stay compact; the tooltip carries the full name.
/// </summary>
public sealed class TransactionStatusLabelTests
{
    [Theory]
    [InlineData("B", "B")]
    [InlineData("SalesOrd:B", "B")]
    [InlineData("PurchOrd:F", "F")]
    [InlineData("Pending Fulfillment", "B")]
    [InlineData("Pending Receipt", "B")]
    public void Display_KeepsLettersCompact(string status, string expected)
    {
        Assert.Equal(expected, TransactionStatusLabel.Display(status));
    }

    [Fact]
    public void Describe_UsesSalesOrderNames_WhenTypeIsSalesOrder()
    {
        Assert.Equal("Pending Fulfillment", TransactionStatusLabel.Describe("B", "Sales Order"));
        Assert.Equal("Pending Billing", TransactionStatusLabel.Describe("SalesOrd:F", "Sales Order"));
    }

    [Fact]
    public void Describe_UsesPurchaseOrderNames_WhenTypeIsPurchaseOrder()
    {
        Assert.Equal("Pending Receipt", TransactionStatusLabel.Describe("B", "Purchase Order"));
        Assert.Equal("Pending Bill", TransactionStatusLabel.Describe("PurchOrd:F"));
    }

    [Fact]
    public void Describe_KeepsFullWords_WhenStatusIsAlreadyAName()
    {
        Assert.Equal("Pending Fulfillment", TransactionStatusLabel.Describe("Pending Fulfillment", "Sales Order"));
    }

    [Fact]
    public void Describe_UsesTransferOrderNames_WhenTypeIsTransferOrder()
    {
        Assert.Equal("Pending Receipt", TransactionStatusLabel.Describe("F", "Transfer Order"));
    }

    [Fact]
    public void Display_ReturnsEmpty_WhenStatusIsMissing()
    {
        Assert.Equal(string.Empty, TransactionStatusLabel.Display(null));
        Assert.Equal(string.Empty, TransactionStatusLabel.Describe("  "));
    }
}
