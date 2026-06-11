using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Shared.Dtos;
using TebiDashboard.Shared.Filters;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// Mock NetSuite implementation with sample TEBI-like data for development and tests.
/// </summary>
public sealed class MockNetSuiteService : INetSuiteService
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CustomerDto>>(GetMockCustomers());

    public Task<IReadOnlyList<CustomerOverviewDto>> GetCustomersOverviewAsync(
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var revenue = GetMockRevenue().Where(r => r.Date >= filter.From && r.Date <= filter.To).ToList();
        var salesOrders = GetMockTransactions()
            .Where(t => t.Type == "Sales Order"
                && !string.IsNullOrWhiteSpace(t.CustomerId)
                && t.TransactionDate >= filter.From
                && t.TransactionDate <= filter.To)
            .ToList();

        var overview = GetMockCustomers()
            .Select(customer =>
            {
                var customerRevenue = revenue.Where(r => r.CustomerId == customer.CustomerId).ToList();
                var customerOrders = salesOrders.Where(t => t.CustomerId == customer.CustomerId).ToList();

                return new CustomerOverviewDto
                {
                    CustomerId = customer.CustomerId,
                    Name = customer.Name,
                    City = customer.City,
                    ContactEmail = customer.ContactEmail,
                    TotalSpent = customerRevenue.Sum(r => r.Amount),
                    RevenueEntryCount = customerRevenue.Count,
                    SalesOrderCount = customerOrders.Count
                };
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<CustomerOverviewDto>>(overview);
    }

    public Task<CustomerDetailDto?> GetCustomerDetailAsync(
        string customerId,
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var customer = GetMockCustomers().FirstOrDefault(c => c.CustomerId == customerId);
        if (customer is null)
        {
            return Task.FromResult<CustomerDetailDto?>(null);
        }

        var revenue = GetMockRevenue()
            .Where(r => r.CustomerId == customerId && r.Date >= filter.From && r.Date <= filter.To)
            .OrderByDescending(r => r.Date)
            .ToList();

        var salesOrders = GetMockTransactions()
            .Where(t => t.Type == "Sales Order"
                && t.CustomerId == customerId
                && t.TransactionDate >= filter.From
                && t.TransactionDate <= filter.To)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        var detail = new CustomerDetailDto
        {
            Customer = customer,
            PeriodFrom = filter.From,
            PeriodTo = filter.To,
            TotalSpent = revenue.Sum(r => r.Amount),
            Revenue = revenue,
            SalesOrders = salesOrders
        };

        return Task.FromResult<CustomerDetailDto?>(detail);
    }

    public Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupplierDto>>(GetMockSuppliers());

    public Task<IReadOnlyList<SupplierOverviewDto>> GetSuppliersOverviewAsync(
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var expenses = GetMockExpenses().Where(e => e.Date >= filter.From && e.Date <= filter.To).ToList();
        var purchaseOrders = GetMockTransactions()
            .Where(t => t.Type == "Purchase Order"
                && !string.IsNullOrWhiteSpace(t.SupplierId)
                && t.TransactionDate >= filter.From
                && t.TransactionDate <= filter.To)
            .ToList();

        var overview = GetMockSuppliers()
            .Select(supplier =>
            {
                var supplierExpenses = expenses.Where(e => e.SupplierId == supplier.SupplierId).ToList();
                var supplierOrders = purchaseOrders.Where(t => t.SupplierId == supplier.SupplierId).ToList();

                return new SupplierOverviewDto
                {
                    SupplierId = supplier.SupplierId,
                    Name = supplier.Name,
                    City = supplier.City,
                    ContactEmail = supplier.ContactEmail,
                    TotalPurchased = supplierExpenses.Sum(e => e.Amount),
                    ExpenseEntryCount = supplierExpenses.Count,
                    PurchaseOrderCount = supplierOrders.Count,
                    PurchasedItemCount = supplierOrders.Select(t => t.ItemId).Distinct().Count()
                };
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<SupplierOverviewDto>>(overview);
    }

    public Task<SupplierDetailDto?> GetSupplierDetailAsync(
        string supplierId,
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var supplier = GetMockSuppliers().FirstOrDefault(s => s.SupplierId == supplierId);
        if (supplier is null)
        {
            return Task.FromResult<SupplierDetailDto?>(null);
        }

        var expenses = GetMockExpenses()
            .Where(e => e.SupplierId == supplierId && e.Date >= filter.From && e.Date <= filter.To)
            .OrderByDescending(e => e.Date)
            .ToList();

        var purchaseOrders = GetMockTransactions()
            .Where(t => t.Type == "Purchase Order"
                && t.SupplierId == supplierId
                && t.TransactionDate >= filter.From
                && t.TransactionDate <= filter.To)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        var purchasedItems = purchaseOrders
            .GroupBy(t => t.ItemId)
            .Select(group => new SupplierPurchasedItemDto
            {
                ItemId = group.Key,
                ItemName = group.First().ItemName,
                TotalQuantityOpen = group.Sum(t => t.QuantityOpen)
            })
            .OrderBy(item => item.ItemName)
            .ToList();

        var detail = new SupplierDetailDto
        {
            Supplier = supplier,
            PeriodFrom = filter.From,
            PeriodTo = filter.To,
            TotalPurchased = expenses.Sum(e => e.Amount),
            Expenses = expenses,
            PurchaseOrders = purchaseOrders,
            PurchasedItems = purchasedItems
        };

        return Task.FromResult<SupplierDetailDto?>(detail);
    }

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var revenue = GetMockRevenue().Where(r => r.Date >= filter.From && r.Date <= filter.To).ToList();
        var transactions = GetMockTransactions().Where(t => t.TransactionDate >= filter.From && t.TransactionDate <= filter.To).ToList();
        var salesVolume = transactions.Sum(t => t.QuantityOpen);
        if (salesVolume == 0)
        {
            salesVolume = revenue.Count;
        }

        var summary = new DashboardSummaryDto
        {
            Revenue = revenue.Sum(r => r.Amount),
            SalesVolume = salesVolume,
            GoalPercentage = 87.5m,
            RevenueTrend = "up",
            LowStockAlertCount = 3,
            PeriodFrom = filter.From,
            PeriodTo = filter.To
        };

        return Task.FromResult(summary);
    }

    public Task<PeriodOverviewDto> GetPeriodOverviewAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var revenue = GetMockRevenue().Where(r => r.Date >= filter.From && r.Date <= filter.To).ToList();
        var expenses = GetMockExpenses().Where(e => e.Date >= filter.From && e.Date <= filter.To).ToList();
        var transactions = GetMockTransactions().Where(t => t.TransactionDate >= filter.From && t.TransactionDate <= filter.To).ToList();
        var lowStock = GetMockLowStock();
        var activeIds = transactions.Select(t => t.ItemId).ToHashSet();

        var customerIdsInPeriod = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.CustomerId))
            .Select(t => t.CustomerId!)
            .ToHashSet();

        var overview = new PeriodOverviewDto
        {
            PeriodFrom = filter.From,
            PeriodTo = filter.To,
            Summary = new DashboardSummaryDto
            {
                Revenue = revenue.Sum(r => r.Amount),
                SalesVolume = transactions.Sum(t => t.QuantityOpen),
                GoalPercentage = 87.5m,
                RevenueTrend = "up",
                LowStockAlertCount = lowStock.Count,
                PeriodFrom = filter.From,
                PeriodTo = filter.To
            },
            Customers = GetMockCustomers().Where(c => customerIdsInPeriod.Contains(c.CustomerId)).ToList(),
            Revenue = revenue,
            Expenses = expenses,
            Transactions = transactions,
            LowStockItems = lowStock,
            InventoryWithActivity = GetMockInventory().Where(i => activeIds.Contains(i.ItemId)).ToList()
        };

        return Task.FromResult(overview);
    }

    public Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var data = GetMockRevenue().Where(r => r.Date >= filter.From && r.Date <= filter.To).ToList();
        return Task.FromResult<IReadOnlyList<RevenueDto>>(data);
    }

    private static List<RevenueDto> GetMockRevenue() =>
    [
        new() { Id = "1", Date = Today.AddDays(-30), Amount = 42000m, CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", ProductGroupId = "pg1" },
        new() { Id = "2", Date = Today.AddDays(-25), Amount = 8200m, CustomerId = "c2", CustomerName = "Gemeente Utrecht", ProductGroupId = "pg2" },
        new() { Id = "3", Date = Today.AddDays(-22), Amount = 15600m, CustomerId = "c3", CustomerName = "Stratenmakers BV", ProductGroupId = "pg1" },
        new() { Id = "4", Date = Today.AddDays(-20), Amount = 6800m, CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", ProductGroupId = "pg1" },
        new() { Id = "5", Date = Today.AddDays(-18), Amount = 22400m, CustomerId = "c4", CustomerName = "Woningbouw De Vliet", ProductGroupId = "pg2" },
        new() { Id = "6", Date = Today.AddDays(-15), Amount = 38500m, CustomerId = "c2", CustomerName = "Gemeente Utrecht", ProductGroupId = "pg2" },
        new() { Id = "7", Date = Today.AddDays(-12), Amount = 14300m, CustomerId = "c3", CustomerName = "Stratenmakers BV", ProductGroupId = "pg1" },
        new() { Id = "8", Date = Today.AddDays(-10), Amount = 9400m, CustomerId = "c5", CustomerName = "Aannemer Noord", ProductGroupId = "pg1" },
        new() { Id = "9", Date = Today.AddDays(-7), Amount = 51200m, CustomerId = "c3", CustomerName = "Stratenmakers BV", ProductGroupId = "pg1" },
        new() { Id = "10", Date = Today.AddDays(-5), Amount = 11200m, CustomerId = "c2", CustomerName = "Gemeente Utrecht", ProductGroupId = "pg2" },
        new() { Id = "11", Date = Today.AddDays(-4), Amount = 8700m, CustomerId = "c4", CustomerName = "Woningbouw De Vliet", ProductGroupId = "pg2" },
        new() { Id = "12", Date = Today.AddDays(-2), Amount = 39500m, CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", ProductGroupId = "pg1" },
        new() { Id = "13", Date = Today.AddDays(-1), Amount = 27800m, CustomerId = "c4", CustomerName = "Woningbouw De Vliet", ProductGroupId = "pg2" }
    ];

    public Task<IReadOnlyList<GoalDto>> GetGoalsAsync(int year, string? period = null, CancellationToken cancellationToken = default)
    {
        var query = GetMockGoals().Where(goal => goal.Year == year);

        if (!string.IsNullOrWhiteSpace(period))
        {
            query = query.Where(goal => goal.Period == period);
        }

        return Task.FromResult<IReadOnlyList<GoalDto>>(query.ToList());
    }

    private static List<GoalDto> GetMockGoals()
    {
        var currentYear = Today.Year;
        var previousYear = currentYear - 1;

        return
        [
            new() { Id = $"g-{previousYear}-q1", Year = previousYear, Period = "Q1", Type = "Revenue", TargetValue = 460_000m, ActualValue = 471_000m, Percentage = 0, Status = "on-track" },
            new() { Id = $"g-{previousYear}-q2", Year = previousYear, Period = "Q2", Type = "Revenue", TargetValue = 480_000m, ActualValue = 462_000m, Percentage = 0, Status = "on-track" },
            new() { Id = $"g-{previousYear}-q3", Year = previousYear, Period = "Q3", Type = "Revenue", TargetValue = 490_000m, ActualValue = 505_000m, Percentage = 0, Status = "ahead" },
            new() { Id = $"g-{previousYear}-q4", Year = previousYear, Period = "Q4", Type = "Revenue", TargetValue = 510_000m, ActualValue = 498_000m, Percentage = 0, Status = "on-track" },
            new() { Id = $"g-{previousYear}-i1-q2", Year = previousYear, Period = "Q2", Type = "Revenue", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", TargetValue = 1150m, ActualValue = 1050m, Percentage = 0, Status = "on-track" },
            new() { Id = $"g-{currentYear}-q1", Year = currentYear, Period = "Q1", Type = "Revenue", TargetValue = 480_000m, ActualValue = 452_000m, Percentage = 0, Status = "on-track" },
            new() { Id = $"g-{currentYear}-q2", Year = currentYear, Period = "Q2", Type = "Revenue", TargetValue = 500_000m, ActualValue = 437_500m, Percentage = 0, Status = "on-track" },
            new() { Id = $"g-{currentYear}-q3", Year = currentYear, Period = "Q3", Type = "Revenue", TargetValue = 520_000m, ActualValue = 0m, Percentage = 0, Status = "off-track" },
            new() { Id = $"g-{currentYear}-q4", Year = currentYear, Period = "Q4", Type = "Revenue", TargetValue = 550_000m, ActualValue = 0m, Percentage = 0, Status = "off-track" },
            new() { Id = $"g-{currentYear}-i1-q2", Year = currentYear, Period = "Q2", Type = "Revenue", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", TargetValue = 1200m, ActualValue = 980m, Percentage = 0, Status = "on-track" }
        ];
    }

    public Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(bool highSalesOnly, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InventoryItemDto>>(GetMockLowStock());

    private static List<InventoryItemDto> GetMockLowStock() =>
    [
        new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 45, ReorderPoint = 100, RecentSalesVolume = 320, IsLowStock = true },
        new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 22, ReorderPoint = 80, RecentSalesVolume = 210, IsLowStock = true }
    ];

    private static List<InventoryItemDto> GetMockInventory() =>
    [
        new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 450, ReorderPoint = 100, RecentSalesVolume = 980, IsLowStock = false },
        new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 22, ReorderPoint = 80, RecentSalesVolume = 210, IsLowStock = true },
        new() { ItemId = "i3", ItemName = "Tegels 60x60 Grijs", Barcode = "8712345000035", QuantityOnHand = 210, ReorderPoint = 50, RecentSalesVolume = 760, IsLowStock = false }
    ];

    public Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InventoryItemDto>>(GetMockInventory());

    public Task<IReadOnlyList<InventoryItemDto>> GetTopSellingItemsAsync(DateRangeFilter filter, int limit, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InventoryItemDto> items =
        [
            new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 450, RecentSalesVolume = 980, IsLowStock = false },
            new() { ItemId = "i3", ItemName = "Tegels 60x60 Grijs", Barcode = "8712345000035", QuantityOnHand = 210, RecentSalesVolume = 760, IsLowStock = false },
            new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 180, RecentSalesVolume = 640, IsLowStock = false }
        ];

        return Task.FromResult<IReadOnlyList<InventoryItemDto>>(items.Take(limit).ToList());
    }

    public Task<IReadOnlyList<StockLocationDto>> GetItemLocationsAsync(string itemId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StockLocationDto> locations =
        [
            new() { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300 },
            new() { LocationId = "loc2", LocationName = "Terrein A", QuantityOnHand = 150 }
        ];

        return Task.FromResult(locations);
    }

    public Task<IReadOnlyList<OpenTransactionDto>> GetOpenTransactionsAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var transactions = GetMockTransactions().Where(t => t.ItemId == itemId).ToList();
        return Task.FromResult<IReadOnlyList<OpenTransactionDto>>(transactions);
    }

    private static List<CustomerDto> GetMockCustomers() =>
    [
        new() { CustomerId = "c1", Name = "Bouwbedrijf Jansen", City = "Utrecht", ContactEmail = "inkoop@jansen.nl" },
        new() { CustomerId = "c2", Name = "Gemeente Utrecht", City = "Utrecht", ContactEmail = "aanbesteding@utrecht.nl" },
        new() { CustomerId = "c3", Name = "Stratenmakers BV", City = "Amersfoort", ContactEmail = "orders@stratenmakers.nl" },
        new() { CustomerId = "c4", Name = "Woningbouw De Vliet", City = "Nieuwegein", ContactEmail = "inkoop@devliet.nl" },
        new() { CustomerId = "c5", Name = "Aannemer Noord", City = "Groningen", ContactEmail = "sales@aannemernoord.nl" }
    ];

    private static List<OpenTransactionDto> GetMockTransactions() =>
    [
        new() { TransactionId = "t1", Type = "Sales Order", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", TransactionDate = Today.AddDays(-1), QuantityOpen = 40, Status = "Pending Fulfillment" },
        new() { TransactionId = "t2", Type = "Transfer Order", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", TransactionDate = Today.AddDays(-2), QuantityOpen = 20, Status = "Pending Receipt" },
        new() { TransactionId = "t3", Type = "Sales Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", CustomerId = "c3", CustomerName = "Stratenmakers BV", TransactionDate = Today.AddDays(-5), QuantityOpen = 55, Status = "Pending Fulfillment" },
        new() { TransactionId = "t4", Type = "Purchase Order", ItemId = "i4", ItemName = "Betonblokken 40x20", SupplierId = "s1", SupplierName = "BetonCentrale Midden-NL", TransactionDate = Today.AddDays(-12), QuantityOpen = 120, Status = "Pending Receipt" },
        new() { TransactionId = "t15", Type = "Purchase Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", SupplierId = "s2", SupplierName = "Klinker Import BV", TransactionDate = Today.AddDays(-20), QuantityOpen = 80, Status = "Pending Receipt" },
        new() { TransactionId = "t16", Type = "Purchase Order", ItemId = "i6", ItemName = "Grind 16-32 mm", SupplierId = "s1", SupplierName = "BetonCentrale Midden-NL", TransactionDate = Today.AddDays(-8), QuantityOpen = 200, Status = "Pending Receipt" }
    ];

    private static List<SupplierDto> GetMockSuppliers() =>
    [
        new() { SupplierId = "s1", Name = "BetonCentrale Midden-NL", City = "Nieuwegein", ContactEmail = "inkoop@betoncentrale.nl" },
        new() { SupplierId = "s2", Name = "Klinker Import BV", City = "Rotterdam", ContactEmail = "orders@klinkerimport.nl" },
        new() { SupplierId = "s3", Name = "Logistics Partner BV", City = "Utrecht", ContactEmail = "transport@logistics.nl" },
        new() { SupplierId = "s4", Name = "Energiebedrijf NL", City = "Arnhem", ContactEmail = "zakelijk@energie.nl" },
        new() { SupplierId = "s5", Name = "Technische Dienst BV", City = "Amersfoort", ContactEmail = "service@technisch.nl" },
        new() { SupplierId = "s6", Name = "TegelGroothandel BV", City = "Utrecht", ContactEmail = "handel@tegelgroothandel.nl" }
    ];

    public Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var expenses = GetMockExpenses().Where(e => e.Date >= filter.From && e.Date <= filter.To).ToList();
        return Task.FromResult<IReadOnlyList<ExpenseDto>>(expenses);
    }

    private static List<ExpenseDto> GetMockExpenses() =>
    [
        new() { Id = "e1", Date = Today.AddDays(-20), Category = "Transport", Amount = 12500m, VendorName = "Logistics Partner BV", SupplierId = "s3" },
        new() { Id = "e2", Date = Today.AddDays(-10), Category = "Utilities", Amount = 4200m, VendorName = "Energiebedrijf NL", SupplierId = "s4" },
        new() { Id = "e3", Date = Today.AddDays(-2), Category = "Maintenance", Amount = 2950m, VendorName = "Technische Dienst BV", SupplierId = "s5" }
    ];

    public Task<ItemDetailDto?> GetItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (barcode != "8712345000011")
        {
            return Task.FromResult<ItemDetailDto?>(null);
        }

        var detail = new ItemDetailDto
        {
            ItemId = "i1",
            ItemName = "Betonstraatstenen Antraciet",
            Barcode = barcode,
            TotalQuantityOnHand = 450,
            ReorderPoint = 100,
            RecentSalesVolume = 980,
            IsLowStock = false,
            Locations =
            [
                new StockLocationDto { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300 },
                new StockLocationDto { LocationId = "loc2", LocationName = "Terrein A", QuantityOnHand = 150 }
            ],
            OpenTransactionCount = 2,
            OpenTransactions =
            [
                new OpenTransactionDto
                {
                    TransactionId = "t1",
                    Type = "Sales Order",
                    ItemId = "i1",
                    ItemName = "Betonstraatstenen Antraciet",
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    TransactionDate = Today.AddDays(-1),
                    QuantityOpen = 40,
                    Status = "Pending Fulfillment"
                }
            ]
        };

        return Task.FromResult<ItemDetailDto?>(detail);
    }

    public Task<ItemDetailDto?> GetItemByIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (itemId != "i1")
        {
            return Task.FromResult<ItemDetailDto?>(null);
        }

        return GetItemByBarcodeAsync("8712345000011", cancellationToken);
    }
}
