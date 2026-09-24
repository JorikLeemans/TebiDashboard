using TebiDashboard.Shared.Dtos;
using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public sealed class ProductGroupLabelTests
{
    [Fact]
    public void For_UsesProductGroupNameWhenPresent()
    {
        var revenue = new RevenueDto
        {
            ProductGroupId = "3",
            ProductGroupName = "Bestrating"
        };

        Assert.Equal("Bestrating", ProductGroupLabel.For(revenue));
    }

    [Fact]
    public void For_FallsBackToIdWhenNameIsMissing()
    {
        var revenue = new RevenueDto { ProductGroupId = "42", ProductGroupName = "  " };

        Assert.Equal("42", ProductGroupLabel.For(revenue));
    }
}
