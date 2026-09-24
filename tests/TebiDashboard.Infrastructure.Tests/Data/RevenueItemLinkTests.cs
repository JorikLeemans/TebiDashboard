using TebiDashboard.Infrastructure.Data;

namespace TebiDashboard.Infrastructure.Tests.Data;

public sealed class RevenueItemLinkTests
{
    [Fact]
    public void ResolveItemName_DoesNotCopyNumericProductGroupIdOntoTheItem()
    {
        var names = new Dictionary<string, string>();

        var itemName = RevenueItemLink.ResolveItemName("16", names, out var itemId);

        Assert.Equal(string.Empty, itemId);
        Assert.Equal(string.Empty, itemName);
    }

    [Fact]
    public void ResolveItemName_UsesInventoryNameForDemoProductGroup()
    {
        var names = new Dictionary<string, string> { ["i5"] = "Opsluitbanden Grijs" };

        var itemName = RevenueItemLink.ResolveItemName("pg-edging", names, out var itemId);

        Assert.Equal("i5", itemId);
        Assert.Equal("Opsluitbanden Grijs", itemName);
    }
}
