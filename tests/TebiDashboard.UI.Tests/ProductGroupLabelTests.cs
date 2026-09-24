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
    public void For_HidesNumericInternalIdsWhenNameIsMissing()
    {
        var revenue = new RevenueDto { ProductGroupId = "16", ProductGroupName = "  " };

        Assert.Equal(string.Empty, ProductGroupLabel.For(revenue));
    }

    [Fact]
    public void For_FallsBackToNonNumericCodeWhenNameIsMissing()
    {
        var revenue = new RevenueDto { ProductGroupId = "pg-edging", ProductGroupName = "  " };

        Assert.Equal("pg-edging", ProductGroupLabel.For(revenue));
    }
}
