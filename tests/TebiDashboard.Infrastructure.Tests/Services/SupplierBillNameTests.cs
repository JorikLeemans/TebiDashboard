using Microsoft.EntityFrameworkCore;
using TebiDashboard.Infrastructure.Data;
using TebiDashboard.Infrastructure.Data.Entities;
using TebiDashboard.Infrastructure.Services;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Infrastructure.Tests.Services;

/// <summary>
/// Vendor bills can point at a supplier that is not in the master table yet.
/// The dashboard must not treat the internal id as the company name, and the detail page must still open.
/// </summary>
public sealed class SupplierBillNameTests
{
    [Fact]
    public async Task Expenses_LeaveSupplierNameEmpty_WhenTheVendorRowIsMissing()
    {
        await using var db = CreateDb();
        db.Expenses.Add(Bill("1279", vendorName: string.Empty));
        await db.SaveChangesAsync();

        var expenses = await new DatabaseNetSuiteService(db).GetExpensesAsync(YearToDate());

        var bill = Assert.Single(expenses);
        Assert.Equal("1279", bill.SupplierId);
        Assert.Equal(string.Empty, bill.SupplierName);
    }

    [Fact]
    public async Task SupplierDetail_OpensBills_WhenTheVendorRowIsMissing()
    {
        await using var db = CreateDb();
        db.Expenses.Add(Bill("1279", vendorName: "L000999 INACTIVE BV"));
        await db.SaveChangesAsync();

        var detail = await new DatabaseNetSuiteService(db).GetSupplierDetailAsync("1279", YearToDate());

        Assert.NotNull(detail);
        Assert.Equal("L000999 INACTIVE BV", detail.Supplier.Name);
        Assert.Equal("1279", detail.Supplier.SupplierId);
        var bill = Assert.Single(detail.Expenses);
        Assert.Equal("bill-1279", bill.Id);
    }

    [Fact]
    public async Task SupplierDetail_ReturnsNull_WhenNothingReferencesTheId()
    {
        await using var db = CreateDb();

        var detail = await new DatabaseNetSuiteService(db).GetSupplierDetailAsync("1279", YearToDate());

        Assert.Null(detail);
    }

    private static ExpenseRecord Bill(string supplierId, string vendorName) => new()
    {
        Id = $"bill-{supplierId}",
        Date = new DateOnly(2026, 3, 6),
        Category = "Vendor Bill",
        Amount = -149.44m,
        VendorName = vendorName,
        SupplierId = supplierId
    };

    private static DateRangeFilter YearToDate() => new()
    {
        From = new DateOnly(2026, 1, 1),
        To = new DateOnly(2026, 10, 5)
    };

    private static TebiDashboardDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TebiDashboardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TebiDashboardDbContext(options);
    }
}
