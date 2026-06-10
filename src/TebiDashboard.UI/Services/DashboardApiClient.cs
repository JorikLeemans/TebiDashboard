using System.Net;
using System.Net.Http.Json;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.UI.Services;

/// <summary>
/// HTTP client wrapper for dashboard API calls from Blazor UI.
/// </summary>
public sealed class DashboardApiClient(HttpClient httpClient)
{
    public async Task<DashboardSummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<DashboardSummaryDto>("api/dashboard/summary", cancellationToken);

    public async Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<RevenueDto>>("api/revenue", cancellationToken) ?? [];

    public async Task<IReadOnlyList<GoalDto>> GetGoalsAsync(int year, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<GoalDto>>($"api/goals?year={year}", cancellationToken) ?? [];

    public async Task<IReadOnlyList<InventoryItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<InventoryItemDto>>("api/inventory/low-stock", cancellationToken) ?? [];

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<InventoryItemDto>>("api/inventory/items", cancellationToken) ?? [];

    public async Task<ItemDetailDto?> GetItemByIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/items/{Uri.EscapeDataString(itemId)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ItemDetailDto>(cancellationToken);
    }

    public async Task<ItemDetailDto?> GetItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<ItemDetailDto>($"api/items/by-barcode/{Uri.EscapeDataString(barcode)}", cancellationToken);
}
