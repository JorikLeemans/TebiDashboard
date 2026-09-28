using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public sealed class ListSortTests
{
    [Fact]
    public void Toggle_FirstClick_SortsAscending()
    {
        var sort = new ListSortState();

        sort.Toggle("name");

        Assert.Equal("name", sort.Column);
        Assert.True(sort.Ascending);
        Assert.Equal("ascending", sort.AriaSort("name"));
        Assert.Equal("none", sort.AriaSort("city"));
    }

    [Fact]
    public void Toggle_SecondClick_ReversesDirection()
    {
        var sort = new ListSortState();

        sort.Toggle("name");
        sort.Toggle("name");

        Assert.False(sort.Ascending);
        Assert.Equal("descending", sort.AriaSort("name"));
    }

    [Fact]
    public void Toggle_DifferentColumn_StartsAscendingAgain()
    {
        var sort = new ListSortState();

        sort.Toggle("name");
        sort.Toggle("name");
        sort.Toggle("city");

        Assert.Equal("city", sort.Column);
        Assert.True(sort.Ascending);
    }

    [Fact]
    public void Apply_SortsStringsCaseInsensitive_ThenReverses()
    {
        var items = new[] { "zeta", "Alpha", "beta" };
        var sort = new ListSortState();
        sort.Toggle("name");

        var ascending = ListSort.Apply(items, sort, value => value);
        sort.Toggle("name");
        var descending = ListSort.Apply(items, sort, value => value);

        Assert.Equal(["Alpha", "beta", "zeta"], ascending);
        Assert.Equal(["zeta", "beta", "Alpha"], descending);
    }

    [Fact]
    public void Apply_SortsNumbers_AndLeavesUnsortedWhenInactive()
    {
        var items = new[] { 30, 10, 20 };
        var inactive = new ListSortState();
        var sort = new ListSortState();
        sort.Toggle("qty");

        Assert.Equal([30, 10, 20], ListSort.Apply(items, inactive, value => value));
        Assert.Equal([10, 20, 30], ListSort.Apply(items, sort, value => value));
    }
}
