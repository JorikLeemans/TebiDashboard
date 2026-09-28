using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public sealed class OpenTransactionLinksTests
{
    [Fact]
    public void ForParty_UsesCustomerPage_ForSalesOrder()
    {
        var line = new OpenTransactionDto
        {
            CustomerId = "c1",
            Type = "Sales Order"
        };

        var href = OpenTransactionLinks.ForParty(line, "t1");

        Assert.Equal("/customers/c1?order=t1", href);
    }

    [Fact]
    public void ForParty_UsesSupplierPage_ForPurchaseOrder()
    {
        var line = new OpenTransactionDto
        {
            SupplierId = "s1",
            Type = "Purchase Order"
        };

        var href = OpenTransactionLinks.ForParty(line, "2136801");

        Assert.Equal("/suppliers/s1?order=2136801", href);
    }

    [Fact]
    public void ForParty_ReturnsNull_ForTransferWithoutParty()
    {
        var line = new OpenTransactionDto { Type = "Transfer Order" };

        Assert.Null(OpenTransactionLinks.ForParty(line, "t2"));
    }

    [Fact]
    public void OrderKeyFromUri_ReadsOrderQuery()
    {
        Assert.Equal("t1", OpenTransactionLinks.OrderKeyFromUri("https://localhost/customers/c1?order=t1"));
        Assert.Equal("t 1", OpenTransactionLinks.OrderKeyFromUri("https://localhost/customers/c1?order=t%201"));
        Assert.Null(OpenTransactionLinks.OrderKeyFromUri("https://localhost/customers/c1"));
    }
}
