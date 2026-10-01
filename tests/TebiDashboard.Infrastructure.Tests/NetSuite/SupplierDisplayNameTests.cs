using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Vendor entityId is the auto-number code; companyName is appended the same way as a company customer.
/// </summary>
public sealed class SupplierDisplayNameTests
{
    [Fact]
    public void From_JoinsEntityIdAndCompanyName()
    {
        Assert.Equal(
            "L12345 BetonCentrale Midden-NL",
            SupplierDisplayName.From("L12345", "BetonCentrale Midden-NL"));
    }

    [Fact]
    public void From_KeepsEntityId_WhenCompanyNameIsBlank()
    {
        Assert.Equal("L12345", SupplierDisplayName.From("L12345", " "));
    }

    [Fact]
    public void From_ReturnsUnknown_WhenEveryPartIsBlank()
    {
        Assert.Equal(SupplierDisplayName.Unknown, SupplierDisplayName.From(null, null));
    }

    [Fact]
    public void From_DoesNotDuplicate_WhenTheCodeIsAlreadyInTheName()
    {
        Assert.Equal(
            "L12345 BetonCentrale Midden-NL",
            SupplierDisplayName.From("L12345", "L12345 BetonCentrale Midden-NL"));
    }
}
