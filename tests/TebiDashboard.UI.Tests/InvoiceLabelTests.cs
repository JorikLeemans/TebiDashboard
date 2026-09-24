using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public class InvoiceLabelTests
{
    [Fact]
    public void For_PrefersInvoiceNumber()
    {
        var label = InvoiceLabel.For(new RevenueDto
        {
            Id = "16-3",
            InvoiceId = "16",
            InvoiceNumber = "F-2026-001"
        });

        Assert.Equal("F-2026-001", label);
    }

    [Fact]
    public void For_FallsBackToInvoiceId()
    {
        var label = InvoiceLabel.For(new RevenueDto
        {
            Id = "16-3",
            InvoiceId = "16"
        });

        Assert.Equal("16", label);
    }

    [Fact]
    public void For_ParsesParentIdFromLineKey()
    {
        var label = InvoiceLabel.For(new RevenueDto { Id = "1842-7" });

        Assert.Equal("1842", label);
    }
}
