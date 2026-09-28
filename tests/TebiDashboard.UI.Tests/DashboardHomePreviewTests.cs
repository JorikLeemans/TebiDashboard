using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

/// <summary>
/// Home dashboard tables show at most 20 newest rows.
/// </summary>
public sealed class DashboardHomePreviewTests
{
    [Fact]
    public void MostRecent_ReturnsAll_WhenAtMostTwenty()
    {
        var items = Enumerable.Range(1, 12).ToList();

        Assert.Equal(items, DashboardHomePreview.MostRecent(items));
    }

    [Fact]
    public void MostRecent_TakesFirstTwenty_WhenLonger()
    {
        var items = Enumerable.Range(1, 50).ToList();

        var preview = DashboardHomePreview.MostRecent(items);

        Assert.Equal(20, preview.Count);
        Assert.Equal(1, preview[0]);
        Assert.Equal(20, preview[^1]);
    }

    [Fact]
    public void TitleCount_ShowsTotalAndPreview()
    {
        Assert.Equal("8", DashboardHomePreview.TitleCount(8, 8));
        Assert.Equal("20 of 1842", DashboardHomePreview.TitleCount(20, 1842));
    }

    [Fact]
    public void TruncationNote_IsEmpty_WhenEverythingFits()
    {
        Assert.Equal(string.Empty, DashboardHomePreview.TruncationNote(12, mostRecent: true));
    }

    [Fact]
    public void TruncationNote_ExplainsMostRecentCap()
    {
        Assert.Equal(
            "Showing the 20 most recent of 25. Period totals stay in the KPIs and chart.",
            DashboardHomePreview.TruncationNote(25, mostRecent: true));
        Assert.Equal("Showing 20 of 40.", DashboardHomePreview.TruncationNote(40, mostRecent: false));
    }
}
