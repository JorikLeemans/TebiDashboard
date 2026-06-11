namespace TebiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Supplier master record for purchase orders and expense reporting.
/// </summary>
public sealed class SupplierRecord
{
    public string SupplierId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;
}
