using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// entityId is the auto-number code; the rest of the label depends on isPerson.
/// </summary>
public sealed class CustomerDisplayNameTests
{
    [Fact]
    public void From_UsesCompanyName_WhenIsPersonIsFalse()
    {
        var name = CustomerDisplayName.From("K12345", "F", "Piet Jansen", "Bouwbedrijf Jansen");

        Assert.Equal("K12345 Bouwbedrijf Jansen", name);
    }

    [Fact]
    public void From_UsesGlommedName_WhenIsPersonIsTrue()
    {
        var name = CustomerDisplayName.From("K67890", "T", "Piet Jansen", "Bouwbedrijf Jansen");

        Assert.Equal("K67890 Piet Jansen", name);
    }

    [Fact]
    public void From_KeepsEntityId_WhenTheTypeSpecificNameIsBlank()
    {
        Assert.Equal("K12345", CustomerDisplayName.From("K12345", "F", "Piet", null));
    }

    [Fact]
    public void From_ReturnsUnknown_WhenEveryPartIsBlank()
    {
        Assert.Equal(CustomerDisplayName.Unknown, CustomerDisplayName.From(null, null, " ", ""));
    }

    [Fact]
    public void From_DoesNotDuplicate_WhenTheCodeIsAlreadyInTheName()
    {
        Assert.Equal("K12345 Bouwbedrijf Jansen", CustomerDisplayName.From("K12345", "F", null, "K12345 Bouwbedrijf Jansen"));
    }
}
