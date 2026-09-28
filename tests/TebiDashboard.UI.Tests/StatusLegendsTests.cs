using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public sealed class StatusLegendsTests
{
    [Theory]
    [InlineData(StatusLegendKind.InventoryStock, "location")]
    [InlineData(StatusLegendKind.SalesOrder, "Pending Fulfillment")]
    [InlineData(StatusLegendKind.PurchaseOrder, "Pending Receipt")]
    [InlineData(StatusLegendKind.OpenTransaction, "Pending Fulfillment")]
    public void GetTooltip_ContainsExpectedStatus(StatusLegendKind kind, string expectedFragment)
    {
        Assert.Contains(expectedFragment, StatusLegends.GetTooltip(kind));
    }
}
