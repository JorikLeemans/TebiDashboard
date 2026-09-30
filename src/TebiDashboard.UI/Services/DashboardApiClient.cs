using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Services;

/// <summary>
/// HTTP client wrapper for dashboard API calls from Blazor UI.
/// Each request sends the signed-in user's JWT.
/// </summary>
public sealed class DashboardApiClient(HttpClient httpClient, IAuthStateService authState)
{
    public Task<DashboardSummaryDto?> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => GetJsonAsync<DashboardSummaryDto>(
            $"api/dashboard/summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public Task<PeriodOverviewDto?> GetPeriodOverviewAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => GetJsonAsync<PeriodOverviewDto>(
            $"api/dashboard/period-overview?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
        => await GetJsonAsync<IReadOnlyList<CustomerDto>>("api/customers", cancellationToken) ?? [];

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
        => await GetJsonAsync<IReadOnlyList<SupplierDto>>("api/suppliers", cancellationToken) ?? [];

    public async Task<IReadOnlyList<SupplierOverviewDto>> GetSuppliersOverviewAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => await GetJsonAsync<IReadOnlyList<SupplierOverviewDto>>(
            $"api/suppliers/overview?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken) ?? [];

    public Task<SupplierDetailDto?> GetSupplierDetailAsync(
        string supplierId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => GetOptionalJsonAsync<SupplierDetailDto>(
            $"api/suppliers/{Uri.EscapeDataString(supplierId)}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public async Task<IReadOnlyList<CustomerOverviewDto>> GetCustomersOverviewAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => await GetJsonAsync<IReadOnlyList<CustomerOverviewDto>>(
            $"api/customers/overview?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken) ?? [];

    public Task<CustomerDetailDto?> GetCustomerDetailAsync(
        string customerId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => GetOptionalJsonAsync<CustomerDetailDto>(
            $"api/customers/{Uri.EscapeDataString(customerId)}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public async Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await GetJsonAsync<IReadOnlyList<RevenueDto>>(
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

        return await GetJsonAsync<IReadOnlyList<GoalDto>>(url, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<GoalDto>> SaveGoalsAsync(
        SaveGoalsRequestDto body,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/goals")
        {
            Content = JsonContent.Create(body)
        };
        AttachBearer(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<GoalDto>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
        => await GetJsonAsync<IReadOnlyList<InventoryItemDto>>("api/inventory/low-stock", cancellationToken) ?? [];

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => await GetJsonAsync<IReadOnlyList<InventoryItemDto>>(
            $"api/inventory/items?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken) ?? [];

    public Task<ItemDetailDto?> GetItemByIdAsync(
        string itemId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => GetOptionalJsonAsync<ItemDetailDto>(
            $"api/items/{Uri.EscapeDataString(itemId)}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public Task<ItemDetailDto?> GetItemByBarcodeAsync(
        string barcode,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => GetJsonAsync<ItemDetailDto>(
            $"api/items/by-barcode/{Uri.EscapeDataString(barcode)}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public Task<TopSellingItemsResultDto?> GetTopSellingItemsAsync(
        DateOnly from,
        DateOnly to,
        int limit = 20,
        string? productGroupId = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/sales-volume/top?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&limit={limit}";
        if (!string.IsNullOrWhiteSpace(productGroupId))
        {
            url += $"&productGroupId={Uri.EscapeDataString(productGroupId)}";
        }

        return GetJsonAsync<TopSellingItemsResultDto>(url, cancellationToken);
    }

    /// <summary>
    /// Short list of articles selling in the period, for advice on the home page.
    /// </summary>
    public async Task<IReadOnlyList<TopSellingItemDto>> GetPopularItemsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => await GetJsonAsync<IReadOnlyList<TopSellingItemDto>>(
            $"api/sales-volume/popular?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken) ?? [];

    private async Task<T?> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    /// <summary>Returns null when the API answers 404, which the detail pages treat as a missing record.</summary>
    private async Task<T?> GetOptionalJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        AttachBearer(request);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private void AttachBearer(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(authState.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Token);
        }
    }
}
