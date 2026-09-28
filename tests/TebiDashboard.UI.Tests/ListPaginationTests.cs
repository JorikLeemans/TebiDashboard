using TebiDashboard.UI.Helpers;

namespace TebiDashboard.UI.Tests;

public sealed class ListPaginationTests
{
    [Fact]
    public void AllowedPageSizes_ContainsExpectedSteps()
    {
        Assert.Equal([5, 10, 20, 25, 50, 100], ListPagination.AllowedPageSizes);
    }

    [Theory]
    [InlineData(0, 25, 1)]
    [InlineData(1, 25, 1)]
    [InlineData(25, 25, 1)]
    [InlineData(26, 25, 2)]
    [InlineData(100, 10, 10)]
    public void GetTotalPages_ReturnsExpectedValue(int totalItems, int pageSize, int expectedPages)
    {
        Assert.Equal(expectedPages, ListPagination.GetTotalPages(totalItems, pageSize));
    }

    [Theory]
    [InlineData(0, 5, 0, 0)]
    [InlineData(12, 5, 1, 5)]
    [InlineData(12, 5, 3, 12)]
    [InlineData(7, 10, 1, 7)]
    public void GetVisibleRange_ReturnsInclusiveBounds(int totalItems, int pageSize, int page, int expectedEnd)
    {
        var (start, end) = ListPagination.GetVisibleRange(page, pageSize, totalItems);

        if (totalItems == 0)
        {
            Assert.Equal(0, start);
            Assert.Equal(0, end);
            return;
        }

        var expectedStart = ((page - 1) * pageSize) + 1;
        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }

    [Fact]
    public void GetPageSlice_ReturnsCorrectWindow()
    {
        var items = Enumerable.Range(1, 12).ToList();

        var page1 = ListPagination.GetPageSlice(items, 1, 5);
        var page3 = ListPagination.GetPageSlice(items, 3, 5);

        Assert.Equal([1, 2, 3, 4, 5], page1);
        Assert.Equal([11, 12], page3);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(5, 1, 1)]
    [InlineData(5, 10, 5)]
    public void ClampPage_StaysWithinBounds(int totalPages, int page, int expected)
    {
        Assert.Equal(expected, ListPagination.ClampPage(page, totalPages));
    }
}
