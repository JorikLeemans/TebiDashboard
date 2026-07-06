using System.Net;
using System.Net.Http.Json;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Services;

/// <summary>
/// HTTP client wrapper for dashboard API calls from Blazor UI.
/// </summary>
public sealed class DashboardApiClient(HttpClient httpClient)
{
    public Task<DashboardSummaryDto?> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<DashboardSummaryDto>(
            $"api/dashboard/summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public Task<PeriodOverviewDto?> GetPeriodOverviewAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<PeriodOverviewDto>(
            $"api/dashboard/period-overview?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<CustomerDto>>("api/customers", cancellationToken) ?? [];

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<SupplierDto>>("api/suppliers", cancellationToken) ?? [];

    public async Task<IReadOnlyList<SupplierOverviewDto>> GetSuppliersOverviewAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<SupplierOverviewDto>>(
            $"api/suppliers/overview?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken) ?? [];

    public Task<SupplierDetailDto?> GetSupplierDetailAsync(
        string supplierId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var response = httpClient.GetAsync(
            $"api/suppliers/{Uri.EscapeDataString(supplierId)}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

        return HandleSupplierDetailResponseAsync(response);
    }

    private static async Task<SupplierDetailDto?> HandleSupplierDetailResponseAsync(Task<HttpResponseMessage> responseTask)
    {
        var response = await responseTask;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SupplierDetailDto>();
    }

    public async Task<IReadOnlyList<CustomerOverviewDto>> GetCustomersOverviewAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<CustomerOverviewDto>>(
            $"api/customers/overview?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken) ?? [];

    public Task<CustomerDetailDto?> GetCustomerDetailAsync(
        string customerId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var response = httpClient.GetAsync(
            $"api/customers/{Uri.EscapeDataString(customerId)}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

        return HandleCustomerDetailResponseAsync(response);
    }

    private static async Task<CustomerDetailDto?> HandleCustomerDetailResponseAsync(Task<HttpResponseMessage> responseTask)
    {
        var response = await responseTask;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDetailDto>();
    }

    public async Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<RevenueDto>>(
            $"api/revenue?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken) ?? [];

    public async Task<IReadOnlyList<GoalDto>> GetGoalsAsync(
        int year,
        string? period = null,
        CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(period)
            ? $"api/goals?year={year}"
            : $"api/goals?year={year}&period={Uri.EscapeDataString(period)}";

        return await httpClient.GetFromJsonAsync<IReadOnlyList<GoalDto>>(url, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<GoalDto>> SaveGoalsAsync(
        SaveGoalsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/goals", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<GoalDto>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<InventoryItemDto>>("api/inventory/low-stock", cancellationToken) ?? [];

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<InventoryItemDto>>(
            $"api/inventory/items?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken) ?? [];

    public Task<ItemDetailDto?> GetItemByIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var response = httpClient.GetAsync($"api/items/{Uri.EscapeDataString(itemId)}", cancellationToken);
        return HandleItemResponseAsync(response);
    }

    public Task<ItemDetailDto?> GetItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<ItemDetailDto>($"api/items/by-barcode/{Uri.EscapeDataString(barcode)}", cancellationToken);

    private static async Task<ItemDetailDto?> HandleItemResponseAsync(Task<HttpResponseMessage> responseTask)
    {
        var response = await responseTask;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ItemDetailDto>();
    }
}
