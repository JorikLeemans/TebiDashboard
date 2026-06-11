using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Application.Interfaces;

/// <summary>
/// Gateway to NetSuite data. Production uses live API; development may use mock.
/// </summary>
public interface INetSuiteService
{
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerOverviewDto>> GetCustomersOverviewAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<CustomerDetailDto?> GetCustomerDetailAsync(string customerId, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierOverviewDto>> GetSuppliersOverviewAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<SupplierDetailDto?> GetSupplierDetailAsync(string supplierId, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<PeriodOverviewDto> GetPeriodOverviewAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoalDto>> GetGoalsAsync(int year, string? period = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(bool highSalesOnly, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItemDto>> GetTopSellingItemsAsync(DateRangeFilter filter, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockLocationDto>> GetItemLocationsAsync(string itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpenTransactionDto>> GetOpenTransactionsAsync(string itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<ItemDetailDto?> GetItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ItemDetailDto?> GetItemByIdAsync(string itemId, CancellationToken cancellationToken = default);
}
