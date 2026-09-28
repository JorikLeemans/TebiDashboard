using TebiDashboard.Application.Interfaces;
using TebiDashboard.Shared.Dtos;

namespace TebiDashboard.Infrastructure.NetSuite;

public static class NetSuiteSyncMapping
{
    public static NetSuiteSyncResultDto ToDto(this NetSuiteSyncResult result) =>
        new()
        {
            Success = result.Success,
            Message = result.Message,
            CustomersSynced = result.CustomersSynced,
            SuppliersSynced = result.SuppliersSynced,
            InventoryItemsSynced = result.InventoryItemsSynced,
            StockLocationsSynced = result.StockLocationsSynced,
            RevenueRecordsSynced = result.RevenueRecordsSynced,
            ExpenseRecordsSynced = result.ExpenseRecordsSynced,
            OpenTransactionsSynced = result.OpenTransactionsSynced,
            CompletedAt = result.CompletedAt
        };

    public static NetSuiteSyncStatusDto ToStatusDto(this NetSuiteSyncRunStatus status) =>
        new()
        {
            State = status.State,
            Message = status.Message,
            Result = status.Result?.ToDto(),
            StartedAt = status.StartedAt,
            CompletedAt = status.CompletedAt,
            CurrentStep = status.CurrentStep,
            CompletedSteps = status.CompletedSteps,
            TotalSteps = status.TotalSteps
        };
}
