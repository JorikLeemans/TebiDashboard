using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public sealed class EntityListFiltersTests
{
    [Fact]
    public void MatchesSearch_ReturnsTrue_WhenQueryIsEmpty()
    {
        Assert.True(EntityListFilters.MatchesSearch(null, "Beton"));
        Assert.True(EntityListFilters.MatchesSearch("  ", "Beton"));
    }

    [Fact]
    public void MatchesSearch_MatchesAnyField_CaseInsensitive()
    {
        Assert.True(EntityListFilters.MatchesSearch("utrecht", "Bouwbedrijf", "Utrecht", "id-1"));
        Assert.True(EntityListFilters.MatchesSearch("8712", "Steen", "8712345000011"));
        Assert.False(EntityListFilters.MatchesSearch("amsterdam", "Utrecht", "id-1"));
    }
}
