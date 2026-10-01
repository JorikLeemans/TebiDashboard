using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.Data;
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
                && !string.IsNullOrWhiteSpace(t.CustomerId))
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
                    TotalSpent = customerOrders.Sum(order => order.Amount),
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
                && t.CustomerId == customerId)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        var detail = new CustomerDetailDto
        {
            Customer = customer,
            PeriodFrom = filter.From,
            PeriodTo = filter.To,
            TotalSpent = salesOrders.Sum(order => order.Amount),
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
                    TotalPurchased = supplierExpenses.Sum(expense => expense.Amount)
                        + supplierOrders.Sum(order => order.Amount),
                    ExpenseEntryCount = supplierExpenses.Count,
                    PurchaseOrderCount = supplierOrders.Count,
                    PurchasedItemCount = supplierOrders.Select(order => order.ItemId).Distinct().Count()
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
            TotalPurchased = expenses.Sum(expense => expense.Amount)
                + purchaseOrders.Sum(order => order.Amount),
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
        var transactions = GetMockTransactions()
            .Where(t => t.Type == "Sales Order"
                || (t.TransactionDate >= filter.From && t.TransactionDate <= filter.To))
            .ToList();
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
        MockRevenue("1", -30, 42000m, "c1", "Bouwbedrijf Jansen", "pg1"),
        MockRevenue("2", -25, 8200m, "c2", "Gemeente Utrecht", "pg2"),
        MockRevenue("3", -22, 15600m, "c3", "Stratenmakers BV", "pg1"),
        MockRevenue("4", -20, 6800m, "c1", "Bouwbedrijf Jansen", "pg1"),
        MockRevenue("5", -18, 22400m, "c4", "Woningbouw De Vliet", "pg2"),
        MockRevenue("6", -15, 38500m, "c2", "Gemeente Utrecht", "pg2"),
        MockRevenue("7", -12, 14300m, "c3", "Stratenmakers BV", "pg1"),
        MockRevenue("8", -10, 9400m, "c5", "Aannemer Noord", "pg1"),
        MockRevenue("9", -7, 51200m, "c3", "Stratenmakers BV", "pg1"),
        MockRevenue("10", -5, 11200m, "c2", "Gemeente Utrecht", "pg2"),
        MockRevenue("11", -4, 8700m, "c4", "Woningbouw De Vliet", "pg2"),
        MockRevenue("12", -2, 39500m, "c1", "Bouwbedrijf Jansen", "pg1"),
        MockRevenue("13", -1, 27800m, "c4", "Woningbouw De Vliet", "pg2")
    ];

    /// <summary>
    /// Mock revenue only stores a product group. Attach the representative item name
    /// the same way the database service does, so detail rows are not just group ids.
    /// </summary>
    private static RevenueDto MockRevenue(
        string id,
        int daysAgo,
        decimal amount,
        string customerId,
        string customerName,
        string productGroupId)
    {
        var itemNames = GetMockInventory().ToDictionary(item => item.ItemId, item => item.ItemName);
        var itemName = RevenueItemLink.ResolveItemName(productGroupId, itemNames, out var itemId);

        return new RevenueDto
        {
            Id = id,
            Date = Today.AddDays(daysAgo),
            Amount = amount,
            CustomerId = customerId,
            CustomerName = customerName,
            ProductGroupId = productGroupId,
            // Same artikelgroep names as the demo seeder so mock mode does not show "pg1".
            ProductGroupName = productGroupId switch
            {
                "pg1" => "Bestrating",
                "pg2" => "Tegels",
                _ => null
            },
            ItemId = itemId,
            ItemName = itemName,
            InvoiceId = customerId,
            InvoiceNumber = $"F-{customerId.ToUpperInvariant()}"
        };
    }

    public Task<IReadOnlyList<GoalDto>> GetGoalsAsync(int year, string? period = null, CancellationToken cancellationToken = default)
    {
        var query = MockGoalData.GetAll().Where(goal => goal.Year == year);

        if (!string.IsNullOrWhiteSpace(period))
        {
            query = query.Where(goal => goal.Period == period);
        }

        return Task.FromResult<IReadOnlyList<GoalDto>>(query.ToList());
    }

    internal static List<GoalDto> CreateInitialGoals()
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

    internal static string GetItemName(string itemId) =>
        GetMockInventory().FirstOrDefault(item => item.ItemId == itemId)?.ItemName ?? itemId;

    public Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(bool highSalesOnly, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InventoryItemDto>>(GetMockLowStock());

    private static List<InventoryItemDto> GetMockLowStock() =>
    [
        new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 45, ReorderPoint = 100, RecentSalesVolume = 320, IsLowStock = true },
        new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 22, ReorderPoint = 80, RecentSalesVolume = 210, IsLowStock = true }
    ];

    private static List<InventoryItemDto> GetMockInventory() =>
    [
        new() { ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", Barcode = "8712345000011", QuantityOnHand = 450, ReorderPoint = 100, RecentSalesVolume = 980, UnitsSold = 420, IsLowStock = false },
        new() { ItemId = "i2", ItemName = "Klinkers Rood-Bruin", Barcode = "8712345000028", QuantityOnHand = 22, ReorderPoint = 80, RecentSalesVolume = 210, UnitsSold = 180, IsLowStock = true },
        new() { ItemId = "i3", ItemName = "Tegels 60x60 Grijs", Barcode = "8712345000035", QuantityOnHand = 210, ReorderPoint = 50, RecentSalesVolume = 760, UnitsSold = 310, IsLowStock = false }
    ];

    public Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(
        DateRangeFilter filter,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InventoryItemDto>>(GetMockInventory());

    public Task<TopSellingItemsResultDto> GetTopSellingItemsAsync(
        DateRangeFilter filter,
        int limit,
        string? productGroupId,
        CancellationToken cancellationToken = default)
    {
        var result = new TopSellingItemsResultDto
        {
            ProductGroups =
            [
                new ProductGroupOptionDto { Id = "pg-paving", Name = "Bestrating" }
            ],
            Items =
            [
                new TopSellingItemDto
                {
                    ItemId = "i1",
                    ItemName = "Betonstraatstenen Antraciet",
                    ProductGroupId = "pg-paving",
                    ProductGroupName = "Bestrating",
                    SalesAmount = 98_000m,
                    UnitsSold = 420m
                },
                new TopSellingItemDto
                {
                    ItemId = "i3",
                    ItemName = "Tegels 60x60 Grijs",
                    ProductGroupId = "pg-paving",
                    ProductGroupName = "Bestrating",
                    SalesAmount = 76_000m,
                    UnitsSold = 310m
                },
                new TopSellingItemDto
                {
                    ItemId = "i2",
                    ItemName = "Klinkers Rood-Bruin",
                    ProductGroupId = "pg-paving",
                    ProductGroupName = "Bestrating",
                    SalesAmount = 64_000m,
                    UnitsSold = 180m
                }
            ]
        };

        var items = string.IsNullOrWhiteSpace(productGroupId)
            ? result.Items
            : result.Items.Where(item => string.Equals(item.ProductGroupId, productGroupId, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult(new TopSellingItemsResultDto
        {
            ProductGroups = result.ProductGroups,
            Items = items.Take(Math.Max(0, limit)).ToList()
        });
    }

    public Task<IReadOnlyList<StockLocationDto>> GetItemLocationsAsync(string itemId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StockLocationDto> locations =
        [
            new() { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300, ReorderPoint = 50, PreferredStockLevel = 80 },
            new() { LocationId = "loc2", LocationName = "Terrein A", QuantityOnHand = 150, ReorderPoint = 50, PreferredStockLevel = 80 }
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
        new() { CustomerId = "c1", Name = "Bouwbedrijf Jansen", City = "Utrecht", ContactEmail = "inkoop@jansen.nl", Category = "Aannemer", Subcategory = "Woningbouw" },
        new() { CustomerId = "c2", Name = "Gemeente Utrecht", City = "Utrecht", ContactEmail = "aanbesteding@utrecht.nl", Category = "Overheid", Subcategory = "Gemeente" },
        new() { CustomerId = "c3", Name = "Stratenmakers BV", City = "Amersfoort", ContactEmail = "orders@stratenmakers.nl", Category = "Stratenmaker", Subcategory = "Infra" },
        new() { CustomerId = "c4", Name = "Woningbouw De Vliet", City = "Nieuwegein", ContactEmail = "inkoop@devliet.nl", Category = "Aannemer", Subcategory = "Woningbouw" },
        new() { CustomerId = "c5", Name = "Aannemer Noord", City = "Groningen", ContactEmail = "sales@aannemernoord.nl", Category = "Aannemer", Subcategory = "Infra" }
    ];

    private static List<OpenTransactionDto> GetMockTransactions() =>
    [
        new() { TransactionId = "t1", Type = "Sales Order", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", TransactionDate = Today.AddDays(-1), QuantityOpen = 40, Amount = 39_500m, Status = "Pending Fulfillment" },
        new() { TransactionId = "t2", Type = "Transfer Order", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", TransactionDate = Today.AddDays(-2), QuantityOpen = 20, Amount = 0m, Status = "Pending Receipt" },
        new() { TransactionId = "t3", Type = "Sales Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", CustomerId = "c3", CustomerName = "Stratenmakers BV", TransactionDate = Today.AddDays(-5), QuantityOpen = 55, Amount = 51_200m, Status = "Pending Fulfillment" },
        new() { TransactionId = "t4", Type = "Purchase Order", ItemId = "i4", ItemName = "Betonblokken 40x20", SupplierId = "s1", SupplierName = "BetonCentrale Midden-NL", TransactionDate = Today.AddDays(-12), QuantityOpen = 120, Amount = 28_800m, Status = "Pending Receipt" },
        new() { TransactionId = "t5", Type = "Sales Order", ItemId = "i3", ItemName = "Tegels 60x60 Grijs", CustomerId = "c2", CustomerName = "Gemeente Utrecht", TransactionDate = Today.AddDays(-3), QuantityOpen = 30, Amount = 11_200m, Status = "Pending Fulfillment" },
        new() { TransactionId = "t-so-12", Type = "Sales Order", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", CustomerId = "c1", CustomerName = "Bouwbedrijf Jansen", TransactionDate = Today.AddDays(-2), QuantityOpen = 47, Amount = 39_500m, Status = "Pending Fulfillment" },
        new() { TransactionId = "t-so-10", Type = "Sales Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", CustomerId = "c2", CustomerName = "Gemeente Utrecht", TransactionDate = Today.AddDays(-5), QuantityOpen = 12, Amount = 11_200m, Status = "Pending Fulfillment" },
        new() { TransactionId = "t-so-9", Type = "Sales Order", ItemId = "i1", ItemName = "Betonstraatstenen Antraciet", CustomerId = "c3", CustomerName = "Stratenmakers BV", TransactionDate = Today.AddDays(-7), QuantityOpen = 60, Amount = 51_200m, Status = "Pending Fulfillment" },
        new() { TransactionId = "t-so-11", Type = "Sales Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", CustomerId = "c4", CustomerName = "Woningbouw De Vliet", TransactionDate = Today.AddDays(-4), QuantityOpen = 24, Amount = 27_800m, Status = "Pending Fulfillment" },
        new() { TransactionId = "t-so-13", Type = "Sales Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", CustomerId = "c4", CustomerName = "Woningbouw De Vliet", TransactionDate = Today.AddDays(-1), QuantityOpen = 24, Amount = 27_800m, Status = "Pending Fulfillment" },
        new() { TransactionId = "t15", Type = "Purchase Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", SupplierId = "s2", SupplierName = "Klinker Import BV", TransactionDate = Today.AddDays(-20), QuantityOpen = 80, Amount = 54_400m, Status = "Pending Receipt" },
        new() { TransactionId = "t16", Type = "Purchase Order", ItemId = "i6", ItemName = "Grind 16-32 mm", SupplierId = "s1", SupplierName = "BetonCentrale Midden-NL", TransactionDate = Today.AddDays(-8), QuantityOpen = 200, Amount = 7_600m, Status = "Pending Receipt" },
        new() { TransactionId = "t17", Type = "Purchase Order", ItemId = "i3", ItemName = "Tegels 60x60 Grijs", SupplierId = "s6", SupplierName = "TegelGroothandel BV", TransactionDate = Today.AddDays(-25), QuantityOpen = 95, Amount = 51_300m, Status = "Pending Receipt" },
        new() { TransactionId = "t18", Type = "Purchase Order", ItemId = "i8", ItemName = "Keramische Tegels Antraciet", SupplierId = "s6", SupplierName = "TegelGroothandel BV", TransactionDate = Today.AddDays(-14), QuantityOpen = 60, Amount = 37_200m, Status = "Pending Receipt" },
        // Recent purchase orders so supplier overview works with the default period filter.
        new() { TransactionId = "t19", Type = "Purchase Order", ItemId = "i4", ItemName = "Betonblokken 40x20", SupplierId = "s1", SupplierName = "BetonCentrale Midden-NL", TransactionDate = Today.AddDays(-2), QuantityOpen = 45, Amount = 10_800m, Status = "Pending Receipt" },
        new() { TransactionId = "t20", Type = "Purchase Order", ItemId = "i2", ItemName = "Klinkers Rood-Bruin", SupplierId = "s2", SupplierName = "Klinker Import BV", TransactionDate = Today.AddDays(-1), QuantityOpen = 30, Amount = 20_400m, Status = "Pending Receipt" },
        new() { TransactionId = "t21", Type = "Purchase Order", ItemId = "i3", ItemName = "Tegels 60x60 Grijs", SupplierId = "s6", SupplierName = "TegelGroothandel BV", TransactionDate = Today, QuantityOpen = 55, Amount = 29_700m, Status = "Pending Receipt" },
        new() { TransactionId = "t22", Type = "Purchase Order", ItemId = "i8", ItemName = "Keramische Tegels Antraciet", SupplierId = "s6", SupplierName = "TegelGroothandel BV", TransactionDate = Today.AddDays(-1), QuantityOpen = 25, Amount = 15_500m, Status = "Pending Receipt" }
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
        MockExpense("e1", -20, "Transport", 12500m, "Logistics Partner BV", "s3"),
        MockExpense("e2", -10, "Utilities", 4200m, "Energiebedrijf NL", "s4"),
        MockExpense("e3", -2, "Maintenance", 2950m, "Technische Dienst BV", "s5"),
        MockExpense("e9", -2, "Transport", 8400m, "Logistics Partner BV", "s3"),
        MockExpense("e10", -1, "Utilities", 5200m, "Energiebedrijf NL", "s4"),
        MockExpense("e11", 0, "Maintenance", 3600m, "Technische Dienst BV", "s5"),
        MockExpense("e12", -2, "Insurance", 4800m, "Verzekeraar NL", "s8")
    ];

    /// <summary>
    /// SupplierName comes from the supplier master when that id exists.
    /// Otherwise keep the vendor text so the expense row still has a readable party name.
    /// </summary>
    private static ExpenseDto MockExpense(
        string id,
        int daysAgo,
        string category,
        decimal amount,
        string vendor,
        string supplierId)
    {
        var supplierName = GetMockSuppliers().FirstOrDefault(supplier => supplier.SupplierId == supplierId)?.Name
            ?? vendor;

        return new ExpenseDto
        {
            Id = id,
            Date = Today.AddDays(daysAgo),
            Category = category,
            Amount = amount,
            VendorName = vendor,
            SupplierId = supplierId,
            SupplierName = supplierName
        };
    }

    public Task<ItemDetailDto?> GetItemByBarcodeAsync(string barcode, DateRangeFilter filter, CancellationToken cancellationToken = default)
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
            PeriodSalesAmount = 12_500m,
            PeriodInvoices =
            [
                new ItemInvoiceLineDto
                {
                    Date = Today.AddDays(-2),
                    InvoiceId = "inv-1",
                    InvoiceNumber = "F-2026-014",
                    CustomerId = "c1",
                    CustomerName = "Bouwbedrijf Jansen",
                    Amount = 8_000m,
                    UnitsSold = 20m
                },
                new ItemInvoiceLineDto
                {
                    Date = Today.AddDays(-1),
                    InvoiceId = "inv-2",
                    InvoiceNumber = "F-2026-018",
                    CustomerId = "c2",
                    CustomerName = "Gemeente Utrecht",
                    Amount = 4_500m,
                    UnitsSold = 12m
                }
            ],
            IsLowStock = false,
            Locations =
            [
                new StockLocationDto { LocationId = "loc1", LocationName = "Hoofdmagazijn", QuantityOnHand = 300, ReorderPoint = 50, PreferredStockLevel = 80 },
                new StockLocationDto { LocationId = "loc2", LocationName = "Terrein A", QuantityOnHand = 150, ReorderPoint = 50, PreferredStockLevel = 80 }
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
                    Amount = 39_500m,
                    Status = "Pending Fulfillment"
                }
            ]
        };

        return Task.FromResult<ItemDetailDto?>(detail);
    }

    public Task<ItemDetailDto?> GetItemByIdAsync(string itemId, DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        if (itemId != "i1")
        {
            return Task.FromResult<ItemDetailDto?>(null);
        }

        return GetItemByBarcodeAsync("8712345000011", filter, cancellationToken);
    }
}
