using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

/// <summary>
/// Groups customer sales-order lines under their parent order.
/// </summary>
public sealed class SalesOrderGroupsTests
{
    [Fact]
    public void From_SumsAmounts_AndKeepsLinesForTheSameOrder()
    {
        var groups = SalesOrderGroups.From(
        [
            new OpenTransactionDto
            {
                TransactionId = "t1",
                TransactionDate = new DateOnly(2026, 9, 23),
                Amount = 39500m,
                QuantityOpen = 40,
                ItemName = "Betonstraatstenen Antraciet",
                Status = "Pending Fulfillment"
            },
            new OpenTransactionDto
            {
                TransactionId = "t1-2",
                TransactionDate = new DateOnly(2026, 9, 23),
                Amount = 14400m,
                QuantityOpen = 12,
                ItemName = "Klinkers Rood-Bruin",
                Status = "Pending Fulfillment"
            },
            new OpenTransactionDto
            {
                TransactionId = "t24",
                TransactionDate = new DateOnly(2026, 9, 24),
                Amount = 21600m,
                QuantityOpen = 18,
                ItemName = "Klinkers Rood-Bruin",
                Status = "Pending Fulfillment"
            }
        ]);

        Assert.Equal(2, groups.Count);
        Assert.Equal("t24", groups[0].OrderLabel);
        Assert.Equal("t1", groups[1].OrderLabel);
        Assert.Equal(53900m, groups[1].TotalAmount);
        Assert.Equal(52m, groups[1].TotalQuantityOpen);
        Assert.Equal(2, groups[1].Lines.Count);
    }

    [Fact]
    public void For_KeepsMockOrderIds_ThatAreNotLineSuffixes()
    {
        var key = SalesOrderLabel.For(new OpenTransactionDto { TransactionId = "t-so-12" });
        Assert.Equal("t-so-12", key);
    }

    [Fact]
    public void For_GroupsNetSuitePurchaseOrderLineIds()
    {
        var key = SalesOrderLabel.For(new OpenTransactionDto { TransactionId = "2136801-4" });
        Assert.Equal("2136801", key);
    }
}
