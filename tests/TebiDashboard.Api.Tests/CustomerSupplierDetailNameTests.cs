using System.Net.Http.Json;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Api.Tests;

public class CustomerSupplierDetailNameTests : IClassFixture<TebiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CustomerSupplierDetailNameTests(TebiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CustomerDetail_IncludesResolvedItemNames_OnRevenueAndSalesOrders()
    {
        var detail = await _client.GetFromJsonAsync<CustomerDetailDto>(
            "/api/customers/c1?from=2000-01-01&to=2100-01-01");

        Assert.NotNull(detail);
        Assert.NotEmpty(detail.Revenue);
        Assert.NotEmpty(detail.SalesOrders);
        Assert.All(detail.Revenue, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.ItemName));
            Assert.NotEqual(row.ProductGroupId, row.ItemName);
        });
        Assert.Contains(detail.Revenue, row => row.ItemName == "Betonstraatstenen Antraciet" && row.ItemId == "i1");
        Assert.Contains(detail.SalesOrders, order => order.ItemName == "Betonstraatstenen Antraciet");
    }

    [Fact]
    public async Task SupplierDetail_IncludesVendorAndItemNames_OnRelatedRows()
    {
        var supplier = await _client.GetFromJsonAsync<SupplierDetailDto>(
            "/api/suppliers/s3?from=2000-01-01&to=2100-01-01");

        Assert.NotNull(supplier);
        Assert.NotEmpty(supplier.Expenses);
        Assert.All(supplier.Expenses, expense =>
        {
            Assert.False(string.IsNullOrWhiteSpace(expense.VendorName));
            Assert.False(string.IsNullOrWhiteSpace(expense.SupplierName));
        });
        Assert.Contains(supplier.Expenses, expense =>
            expense.VendorName == "Logistics Partner BV"
            && expense.SupplierName == "Logistics Partner BV");

        var purchaser = await _client.GetFromJsonAsync<SupplierDetailDto>(
            "/api/suppliers/s1?from=2000-01-01&to=2100-01-01");

        Assert.NotNull(purchaser);
        Assert.Contains(purchaser.PurchaseOrders, order => order.ItemName == "Betonblokken 40x20" && order.ItemId == "i4");
        Assert.Contains(purchaser.PurchasedItems, item => item.ItemName == "Betonblokken 40x20" && item.ItemId == "i4");
    }
}
