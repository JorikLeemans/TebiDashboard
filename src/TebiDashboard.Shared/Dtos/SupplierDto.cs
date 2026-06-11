namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Supplier master data for dashboard and purchase order views.
/// </summary>
public sealed class SupplierDto
{
    public string SupplierId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string ContactEmail { get; init; } = string.Empty;
}
